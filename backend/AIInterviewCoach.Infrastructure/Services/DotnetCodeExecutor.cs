using System.Buffers;
using System.Diagnostics;
using System.Text;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIInterviewCoach.Infrastructure.Services
{
    public sealed class DotnetCodeExecutor : ICodeExecutor
    {
        private const string DefaultLanguage = "csharp";
        private const int MaxSourceCodeCharacters = 100_000;
        private const int MaxWrappedSourceCodeCharacters = 150_000;
        private const int MaxTestCaseCount = 128;
        private const int MaxStandardInputCharacters = 64_000;
        private const int MaxExpectedOutputCharacters = 64_000;
        private const int MaxProcessStreamCharacters = 64_000;

        private static readonly TimeSpan RestoreTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan BuildTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan WorkspaceCleanupAge = TimeSpan.FromHours(12);

        private static readonly IReadOnlyDictionary<string, string[]> RestrictedApiPatterns =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [DefaultLanguage] =
                [
                    "System.IO",
                    "File.",
                    "Directory.",
                    "Environment.",
                    "System.Diagnostics",
                    "Process.",
                    "HttpClient",
                    "WebRequest",
                    "System.Net",
                    "Socket",
                    "Dns.",
                    "TcpClient",
                    "UdpClient",
                    "DllImport",
                    "Marshal.",
                    "Registry."
                ],
                ["python"] =
                [
                    "import os",
                    "from os",
                    "import subprocess",
                    "from subprocess",
                    "import socket",
                    "from socket",
                    "import pathlib",
                    "from pathlib",
                    "import shutil",
                    "from shutil",
                    "import requests",
                    "from requests",
                    "import urllib",
                    "from urllib",
                    "import ctypes",
                    "from ctypes",
                    "import multiprocessing",
                    "from multiprocessing",
                    "open(",
                    "__import__(\"os\"",
                    "__import__('os'",
                    "eval(",
                    "exec(",
                    "compile("
                ],
                ["cpp"] =
                [
                    "#include <fstream>",
                    "#include <filesystem>",
                    "std::filesystem",
                    "#include <thread>",
                    "#include <future>",
                    "#include <unistd.h>",
                    "#include <windows.h>",
                    "#include <sys/socket.h>",
                    "#include <netdb.h>",
                    "#include <curl/",
                    "CreateFile(",
                    "system(",
                    "popen(",
                    "fork("
                ]
            };

        private readonly string _workspaceRoot;
        private readonly IObservabilityService _observabilityService;
        private readonly ILogger<DotnetCodeExecutor> _logger;

        public DotnetCodeExecutor()
            : this(new ApplicationObservabilityService(), NullLogger<DotnetCodeExecutor>.Instance)
        {
        }

        public DotnetCodeExecutor(
            IObservabilityService observabilityService,
            ILogger<DotnetCodeExecutor> logger)
        {
            _observabilityService = observabilityService;
            _logger = logger;
            _workspaceRoot = Path.Combine(Path.GetTempPath(), "ai-interview-coach-executor");
            Directory.CreateDirectory(_workspaceRoot);
            CleanupStaleWorkspaces();
        }

        public async Task<ExecutionResult> ExecuteAsync(
            Problem problem,
            string code,
            string language,
            IEnumerable<TestCase> testCases)
        {
            var stopwatch = Stopwatch.StartNew();
            var normalizedLanguage = NormalizeLanguage(language);
            var normalizedExecutionMode = NormalizeExecutionMode(problem.ExecutionMode);
            var orderedTests = testCases
                .OrderBy(testCase => testCase.OrderIndex)
                .ToList();
            ExecutionResult? result = null;

            var workspacePath = Path.Combine(_workspaceRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workspacePath);
            var sandbox = CreateSandboxContext(workspacePath);

            try
            {
                if (normalizedLanguage is null)
                {
                    result = BuildRejectedResult(
                        SubmissionStatus.CompilationError,
                        "Unsupported language. Supported languages are C#, Python, and C++.",
                        orderedTests.Count);
                    return result;
                }

                var validationError = ValidateExecutionRequest(
                    code,
                    normalizedLanguage,
                    orderedTests);

                if (validationError is not null)
                {
                    result = BuildRejectedResult(
                        SubmissionStatus.CompilationError,
                        validationError,
                        orderedTests.Count);
                    return result;
                }

                if (orderedTests.Count == 0)
                {
                    result = new ExecutionResult(
                        SubmissionStatus.Accepted,
                        "Accepted. No test cases configured.",
                        0,
                        0,
                        0,
                        null);
                    return result;
                }

                result = normalizedLanguage switch
                {
                    DefaultLanguage => await ExecuteCSharpAsync(
                        problem,
                        normalizedExecutionMode,
                        code,
                        orderedTests,
                        sandbox),
                    "python" => await ExecutePythonAsync(
                        problem,
                        normalizedExecutionMode,
                        code,
                        orderedTests,
                        sandbox),
                    "cpp" => await ExecuteCppAsync(
                        problem,
                        normalizedExecutionMode,
                        code,
                        orderedTests,
                        sandbox),
                    _ => BuildRejectedResult(
                        SubmissionStatus.CompilationError,
                        "Unsupported language. Supported languages are C#, Python, and C++.",
                        orderedTests.Count)
                };

                return result;
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                _observabilityService.RecordCodeExecution(
                    normalizedLanguage ?? "unknown",
                    normalizedExecutionMode,
                    "UnhandledException",
                    stopwatch.Elapsed,
                    orderedTests.Count);

                _logger.LogError(
                    exception,
                    "Unhandled code execution failure for problem {ProblemId} using {Language}.",
                    problem.Id,
                    normalizedLanguage ?? language);

                throw;
            }
            finally
            {
                if (result is not null)
                {
                    stopwatch.Stop();
                    _observabilityService.RecordCodeExecution(
                        normalizedLanguage ?? "unknown",
                        normalizedExecutionMode,
                        result.Status.ToString(),
                        stopwatch.Elapsed,
                        orderedTests.Count);
                }

                TryDeleteDirectory(workspacePath);
            }
        }

        private static string NormalizeExecutionMode(string executionMode)
        {
            return executionMode.Trim().ToLowerInvariant() == ProblemExecutionModes.FunctionSignature
                ? ProblemExecutionModes.FunctionSignature
                : ProblemExecutionModes.Stdin;
        }

        private static string? NormalizeLanguage(string language)
        {
            return language.Trim().ToLowerInvariant() switch
            {
                "csharp" or "cs" or "c#" => DefaultLanguage,
                "python" or "py" => "python",
                "cpp" or "c++" or "cc" or "cxx" => "cpp",
                _ => null
            };
        }

        private static string? ValidateExecutionRequest(
            string code,
            string language,
            IReadOnlyList<TestCase> orderedTests)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return "Submission source code is empty.";
            }

            if (code.Length > MaxSourceCodeCharacters)
            {
                return
                    $"Submission source code exceeds the {MaxSourceCodeCharacters:N0}-character limit.";
            }

            if (orderedTests.Count > MaxTestCaseCount)
            {
                return
                    $"This problem has too many test cases configured. The execution layer supports up to {MaxTestCaseCount} test cases per run.";
            }

            if (orderedTests.Any(testCase => (testCase.Input?.Length ?? 0) > MaxStandardInputCharacters))
            {
                return
                    $"A configured test case exceeds the {MaxStandardInputCharacters:N0}-character input limit.";
            }

            if (orderedTests.Any(testCase => (testCase.ExpectedOutput?.Length ?? 0) > MaxExpectedOutputCharacters))
            {
                return
                    $"A configured test case exceeds the {MaxExpectedOutputCharacters:N0}-character expected output limit.";
            }

            var restrictedPattern = FindRestrictedPattern(code, language);

            if (restrictedPattern is not null)
            {
                return
                    $"Submission uses restricted API '{restrictedPattern}'. Only in-memory computation with stdin/stdout is allowed.";
            }

            return null;
        }

        private static string? FindRestrictedPattern(string code, string language)
        {
            if (!RestrictedApiPatterns.TryGetValue(language, out var patterns))
            {
                return null;
            }

            foreach (var pattern in patterns)
            {
                if (code.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return pattern;
                }
            }

            return null;
        }

        private static async Task<ExecutionResult> ExecuteCSharpAsync(
            Problem problem,
            string executionMode,
            string code,
            IReadOnlyList<TestCase> orderedTests,
            ExecutionSandboxContext sandbox)
        {
            var sourceCodeResult = BuildSourceCode(problem, executionMode, DefaultLanguage, code);
            if (sourceCodeResult.ErrorMessage is not null)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    sourceCodeResult.ErrorMessage,
                    orderedTests.Count);
            }

            await WriteSandboxFileAsync(
                Path.Combine(sandbox.WorkspacePath, "SubmissionRunner.csproj"),
                SubmissionProjectTemplate);
            await WriteSandboxFileAsync(
                Path.Combine(sandbox.WorkspacePath, "Program.cs"),
                sourceCodeResult.SourceCode);

            var restoreResult = await RunProcessAsync(
                "dotnet",
                ["restore", "--nologo", "--ignore-failed-sources", "--packages", sandbox.NugetPackagesDirectory],
                sandbox,
                null,
                RestoreTimeout);

            if (restoreResult.TimedOut)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    "Dependency restore timed out.",
                    orderedTests.Count);
            }

            if (restoreResult.ExitCode != 0)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    BuildOutputMessage(restoreResult),
                    orderedTests.Count);
            }

            var buildResult = await RunProcessAsync(
                "dotnet",
                ["build", "--no-restore", "--nologo", "--verbosity", "quiet", "-p:UseSharedCompilation=false"],
                sandbox,
                null,
                BuildTimeout);

            if (buildResult.TimedOut)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    "Compilation timed out.",
                    orderedTests.Count);
            }

            if (buildResult.ExitCode != 0)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    BuildOutputMessage(buildResult),
                    orderedTests.Count);
            }

            var assemblyPath = Path.Combine(
                sandbox.WorkspacePath,
                "bin",
                "Debug",
                "net10.0",
                "SubmissionRunner.dll");

            return await RunAgainstTestsAsync(
                "dotnet",
                [assemblyPath],
                sandbox,
                orderedTests);
        }

        private static async Task<ExecutionResult> ExecutePythonAsync(
            Problem problem,
            string executionMode,
            string code,
            IReadOnlyList<TestCase> orderedTests,
            ExecutionSandboxContext sandbox)
        {
            var pythonCommand = ResolveAvailableCommand("python3", "python");

            if (pythonCommand is null)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    "Python is not available on the server.",
                    orderedTests.Count);
            }

            var sourceCodeResult = BuildSourceCode(problem, executionMode, "python", code);
            if (sourceCodeResult.ErrorMessage is not null)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    sourceCodeResult.ErrorMessage,
                    orderedTests.Count);
            }

            var scriptPath = Path.Combine(sandbox.WorkspacePath, "main.py");
            await WriteSandboxFileAsync(scriptPath, sourceCodeResult.SourceCode);

            var syntaxCheckResult = await RunProcessAsync(
                pythonCommand,
                ["-I", "-m", "py_compile", scriptPath],
                sandbox,
                null,
                BuildTimeout);

            if (syntaxCheckResult.TimedOut)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    "Compilation timed out.",
                    orderedTests.Count);
            }

            if (syntaxCheckResult.ExitCode != 0)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    BuildOutputMessage(syntaxCheckResult),
                    orderedTests.Count);
            }

            return await RunAgainstTestsAsync(
                pythonCommand,
                ["-I", scriptPath],
                sandbox,
                orderedTests);
        }

        private static async Task<ExecutionResult> ExecuteCppAsync(
            Problem problem,
            string executionMode,
            string code,
            IReadOnlyList<TestCase> orderedTests,
            ExecutionSandboxContext sandbox)
        {
            var compilerCommand = ResolveAvailableCommand("c++", "clang++", "g++");

            if (compilerCommand is null)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    "A C++ compiler is not available on the server.",
                    orderedTests.Count);
            }

            var sourceCodeResult = BuildSourceCode(problem, executionMode, "cpp", code);
            if (sourceCodeResult.ErrorMessage is not null)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    sourceCodeResult.ErrorMessage,
                    orderedTests.Count);
            }

            var sourcePath = Path.Combine(sandbox.WorkspacePath, "main.cpp");
            var binaryPath = Path.Combine(sandbox.WorkspacePath, "SubmissionRunner");

            await WriteSandboxFileAsync(sourcePath, sourceCodeResult.SourceCode);

            var buildResult = await RunProcessAsync(
                compilerCommand,
                ["-std=c++17", "-O2", sourcePath, "-o", binaryPath],
                sandbox,
                null,
                BuildTimeout);

            if (buildResult.TimedOut)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    "Compilation timed out.",
                    orderedTests.Count);
            }

            if (buildResult.ExitCode != 0)
            {
                return BuildRejectedResult(
                    SubmissionStatus.CompilationError,
                    BuildOutputMessage(buildResult),
                    orderedTests.Count);
            }

            return await RunAgainstTestsAsync(
                binaryPath,
                [],
                sandbox,
                orderedTests);
        }

        private static async Task<ExecutionResult> RunAgainstTestsAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            ExecutionSandboxContext sandbox,
            IReadOnlyList<TestCase> orderedTests)
        {
            var totalExecutionTimeMs = 0;
            var passedTests = 0;

            foreach (var testCase in orderedTests)
            {
                var executionResult = await RunProcessAsync(
                    fileName,
                    arguments,
                    sandbox,
                    testCase.Input,
                    ExecutionTimeout);

                if (executionResult.TimedOut)
                {
                    return new ExecutionResult(
                        SubmissionStatus.TimeLimitExceeded,
                        "Execution timed out.",
                        passedTests,
                        orderedTests.Count,
                        totalExecutionTimeMs,
                        null);
                }

                if (executionResult.StreamLimitExceeded)
                {
                    return new ExecutionResult(
                        SubmissionStatus.RuntimeError,
                        $"Execution output exceeded the {MaxProcessStreamCharacters:N0}-character safety limit.",
                        passedTests,
                        orderedTests.Count,
                        totalExecutionTimeMs,
                        null);
                }

                totalExecutionTimeMs += executionResult.ElapsedMilliseconds;

                if (executionResult.ExitCode != 0)
                {
                    return new ExecutionResult(
                        SubmissionStatus.RuntimeError,
                        BuildOutputMessage(executionResult),
                        passedTests,
                        orderedTests.Count,
                        totalExecutionTimeMs,
                        null);
                }

                var actualOutput = NormalizeOutput(executionResult.StandardOutput);
                var expectedOutput = NormalizeOutput(testCase.ExpectedOutput);

                if (!string.Equals(actualOutput, expectedOutput, StringComparison.Ordinal))
                {
                    return new ExecutionResult(
                        SubmissionStatus.WrongAnswer,
                        $"Expected output '{expectedOutput}' but received '{actualOutput}'.",
                        passedTests,
                        orderedTests.Count,
                        totalExecutionTimeMs,
                        null);
                }

                passedTests++;
            }

            return new ExecutionResult(
                SubmissionStatus.Accepted,
                "Accepted.",
                passedTests,
                orderedTests.Count,
                totalExecutionTimeMs,
                null);
        }

        private static async Task<ProcessExecutionResult> RunProcessAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            ExecutionSandboxContext sandbox,
            string? standardInput,
            TimeSpan timeout)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = sandbox.WorkspacePath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            ConfigureSandboxEnvironment(startInfo, sandbox);

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            var outputTask = ReadStreamWithLimitAsync(
                process.StandardOutput,
                MaxProcessStreamCharacters);
            var errorTask = ReadStreamWithLimitAsync(
                process.StandardError,
                MaxProcessStreamCharacters);

            if (!string.IsNullOrEmpty(standardInput))
            {
                await process.StandardInput.WriteAsync(standardInput);
            }

            process.StandardInput.Close();

            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var timeoutCts = new CancellationTokenSource(timeout);
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                TryKillProcess(process);
                await process.WaitForExitAsync();

                stopwatch.Stop();

                var timedOutOutput = await outputTask;
                var timedOutError = await errorTask;

                return new ProcessExecutionResult(
                    ExitCode: null,
                    StandardOutput: timedOutOutput.Content,
                    StandardError: timedOutError.Content,
                    TimedOut: true,
                    ElapsedMilliseconds: (int)stopwatch.ElapsedMilliseconds,
                    StreamLimitExceeded: timedOutOutput.WasTruncated || timedOutError.WasTruncated);
            }

            stopwatch.Stop();

            var output = await outputTask;
            var error = await errorTask;

            return new ProcessExecutionResult(
                ExitCode: process.ExitCode,
                StandardOutput: output.Content,
                StandardError: error.Content,
                TimedOut: false,
                ElapsedMilliseconds: (int)stopwatch.ElapsedMilliseconds,
                StreamLimitExceeded: output.WasTruncated || error.WasTruncated);
        }

        private static async Task<BoundedReadResult> ReadStreamWithLimitAsync(
            StreamReader reader,
            int characterLimit)
        {
            var buffer = ArrayPool<char>.Shared.Rent(1024);
            var builder = new StringBuilder(Math.Min(characterLimit, 4096));
            var wasTruncated = false;

            try
            {
                while (true)
                {
                    var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length));
                    if (read == 0)
                    {
                        break;
                    }

                    if (builder.Length < characterLimit)
                    {
                        var remaining = characterLimit - builder.Length;
                        var toAppend = Math.Min(remaining, read);
                        builder.Append(buffer, 0, toAppend);

                        if (toAppend < read)
                        {
                            wasTruncated = true;
                        }
                    }
                    else
                    {
                        wasTruncated = true;
                    }
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }

            return new BoundedReadResult(builder.ToString(), wasTruncated);
        }

        private static void ConfigureSandboxEnvironment(
            ProcessStartInfo startInfo,
            ExecutionSandboxContext sandbox)
        {
            startInfo.Environment.Clear();

            CopyEnvironmentVariableIfPresent(startInfo, "PATH");
            CopyEnvironmentVariableIfPresent(startInfo, "PATHEXT");
            CopyEnvironmentVariableIfPresent(startInfo, "SystemRoot");
            CopyEnvironmentVariableIfPresent(startInfo, "WINDIR");
            CopyEnvironmentVariableIfPresent(startInfo, "COMSPEC");

            startInfo.Environment["HOME"] = sandbox.HomeDirectory;
            startInfo.Environment["USERPROFILE"] = sandbox.HomeDirectory;
            startInfo.Environment["TMPDIR"] = sandbox.TempDirectory;
            startInfo.Environment["TMP"] = sandbox.TempDirectory;
            startInfo.Environment["TEMP"] = sandbox.TempDirectory;
            startInfo.Environment["DOTNET_CLI_HOME"] = sandbox.HomeDirectory;
            startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
            startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            startInfo.Environment["DOTNET_NOLOGO"] = "1";
            startInfo.Environment["DOTNET_EnableDiagnostics"] = "0";
            startInfo.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            startInfo.Environment["MSBUILDNOINPROCNODE"] = "1";
            startInfo.Environment["NUGET_PACKAGES"] = sandbox.NugetPackagesDirectory;
            startInfo.Environment["NUGET_HTTP_CACHE_PATH"] = sandbox.NugetHttpCacheDirectory;
            startInfo.Environment["NUGET_PLUGINS_CACHE_PATH"] = sandbox.NugetPluginsCacheDirectory;
            startInfo.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
            startInfo.Environment["PYTHONNOUSERSITE"] = "1";
            startInfo.Environment["PIP_DISABLE_PIP_VERSION_CHECK"] = "1";
        }

        private static void CopyEnvironmentVariableIfPresent(
            ProcessStartInfo startInfo,
            string key)
        {
            var value = Environment.GetEnvironmentVariable(key);

            if (!string.IsNullOrWhiteSpace(value))
            {
                startInfo.Environment[key] = value;
            }
        }

        private static void TryKillProcess(Process process)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }

        private static string BuildOutputMessage(ProcessExecutionResult result)
        {
            var message = string.IsNullOrWhiteSpace(result.StandardError)
                ? result.StandardOutput
                : result.StandardError;

            if (string.IsNullOrWhiteSpace(message))
            {
                message = "The submission did not produce any diagnostic output.";
            }
            else
            {
                message = message.Trim();
            }

            if (result.StreamLimitExceeded)
            {
                message = $"{message}{Environment.NewLine}{Environment.NewLine}Diagnostic output was truncated for safety.";
            }

            return message;
        }

        private static string NormalizeOutput(string value) =>
            value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

        private static SourceCodeBuildResult BuildSourceCode(
            Problem problem,
            string executionMode,
            string language,
            string candidateCode)
        {
            if (executionMode != ProblemExecutionModes.FunctionSignature)
            {
                return new SourceCodeBuildResult(candidateCode, null);
            }

            var harnessTemplate = language switch
            {
                DefaultLanguage => problem.CsharpHarnessTemplate,
                "python" => problem.PythonHarnessTemplate,
                "cpp" => problem.CppHarnessTemplate,
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(harnessTemplate))
            {
                return new SourceCodeBuildResult(
                    string.Empty,
                    $"This problem is missing its hidden {FormatLanguage(language)} harness template.");
            }

            if (!harnessTemplate.Contains("{{candidate_code}}", StringComparison.Ordinal))
            {
                return new SourceCodeBuildResult(
                    string.Empty,
                    $"The hidden {FormatLanguage(language)} harness template is invalid because it does not contain the {{candidate_code}} placeholder.");
            }

            var wrappedSource = harnessTemplate.Replace(
                "{{candidate_code}}",
                candidateCode,
                StringComparison.Ordinal);

            if (wrappedSource.Length > MaxWrappedSourceCodeCharacters)
            {
                return new SourceCodeBuildResult(
                    string.Empty,
                    $"Wrapped submission source exceeds the {MaxWrappedSourceCodeCharacters:N0}-character limit.");
            }

            return new SourceCodeBuildResult(wrappedSource, null);
        }

        private static string? ResolveAvailableCommand(params string[] commandNames)
        {
            var pathValue = Environment.GetEnvironmentVariable("PATH");

            if (string.IsNullOrWhiteSpace(pathValue))
            {
                return null;
            }

            var pathEntries = pathValue.Split(
                Path.PathSeparator,
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var commandName in commandNames)
            {
                if (Path.IsPathRooted(commandName) && File.Exists(commandName))
                {
                    return commandName;
                }

                foreach (var pathEntry in pathEntries)
                {
                    var fullPath = Path.Combine(pathEntry, commandName);

                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }

            return null;
        }

        private static ExecutionResult BuildRejectedResult(
            SubmissionStatus status,
            string message,
            int totalTests)
        {
            return new ExecutionResult(
                status,
                message,
                0,
                totalTests,
                null,
                null);
        }

        private static ExecutionSandboxContext CreateSandboxContext(string workspacePath)
        {
            var homeDirectory = Path.Combine(workspacePath, "home");
            var tempDirectory = Path.Combine(workspacePath, "tmp");
            var nugetPackagesDirectory = Path.Combine(workspacePath, "nuget-packages");
            var nugetHttpCacheDirectory = Path.Combine(workspacePath, "nuget-http-cache");
            var nugetPluginsCacheDirectory = Path.Combine(workspacePath, "nuget-plugins-cache");

            CreateSandboxDirectory(homeDirectory);
            CreateSandboxDirectory(tempDirectory);
            CreateSandboxDirectory(nugetPackagesDirectory);
            CreateSandboxDirectory(nugetHttpCacheDirectory);
            CreateSandboxDirectory(nugetPluginsCacheDirectory);

            return new ExecutionSandboxContext(
                workspacePath,
                homeDirectory,
                tempDirectory,
                nugetPackagesDirectory,
                nugetHttpCacheDirectory,
                nugetPluginsCacheDirectory);
        }

        private void CleanupStaleWorkspaces()
        {
            try
            {
                foreach (var directory in Directory.EnumerateDirectories(_workspaceRoot))
                {
                    var createdAtUtc = Directory.GetCreationTimeUtc(directory);

                    if (createdAtUtc < DateTime.UtcNow.Subtract(WorkspaceCleanupAge))
                    {
                        TryDeleteDirectory(directory);
                    }
                }
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }

        private static void TryDeleteDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup; stale temp folders are less harmful than failing the request.
            }
        }

        private static void CreateSandboxDirectory(string directoryPath)
        {
            if (OperatingSystem.IsWindows())
            {
                Directory.CreateDirectory(directoryPath);
                return;
            }

            Directory.CreateDirectory(
                directoryPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        private static async Task WriteSandboxFileAsync(string filePath, string content)
        {
            if (OperatingSystem.IsWindows())
            {
                await File.WriteAllTextAsync(filePath, content);
                return;
            }

            await using var stream = new FileStream(
                filePath,
                new FileStreamOptions
                {
                    Mode = FileMode.Create,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous,
                    UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
                });

            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(content);
        }

        private const string SubmissionProjectTemplate =
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <AssemblyName>SubmissionRunner</AssemblyName>
              </PropertyGroup>
            </Project>
            """;

        private static string FormatLanguage(string language) =>
            language switch
            {
                "python" => "Python",
                "cpp" => "C++",
                _ => "C#"
            };

        private sealed record ExecutionSandboxContext(
            string WorkspacePath,
            string HomeDirectory,
            string TempDirectory,
            string NugetPackagesDirectory,
            string NugetHttpCacheDirectory,
            string NugetPluginsCacheDirectory);

        private sealed record ProcessExecutionResult(
            int? ExitCode,
            string StandardOutput,
            string StandardError,
            bool TimedOut,
            int ElapsedMilliseconds,
            bool StreamLimitExceeded);

        private sealed record BoundedReadResult(
            string Content,
            bool WasTruncated);

        private sealed record SourceCodeBuildResult(
            string SourceCode,
            string? ErrorMessage);
    }
}
