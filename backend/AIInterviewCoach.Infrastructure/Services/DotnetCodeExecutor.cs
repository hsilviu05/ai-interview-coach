using System.Diagnostics;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Infrastructure.Services
{
    public sealed class DotnetCodeExecutor : ICodeExecutor
    {
        private const string DefaultLanguage = "csharp";
        private static readonly TimeSpan RestoreTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan BuildTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(2);

        private readonly string _workspaceRoot;

        public DotnetCodeExecutor()
        {
            _workspaceRoot = Path.Combine(Path.GetTempPath(), "ai-interview-coach-executor");
            Directory.CreateDirectory(_workspaceRoot);
        }

        public async Task<ExecutionResult> ExecuteAsync(
            Problem problem,
            string code,
            string language,
            IEnumerable<TestCase> testCases)
        {
            var normalizedLanguage = NormalizeLanguage(language);
            var normalizedExecutionMode = NormalizeExecutionMode(problem.ExecutionMode);
            var orderedTests = testCases
                .OrderBy(testCase => testCase.OrderIndex)
                .ToList();

            if (normalizedLanguage is null)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    "Unsupported language. Supported languages are C#, Python, and C++.",
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            if (orderedTests.Count == 0)
            {
                return new ExecutionResult(
                    SubmissionStatus.Accepted,
                    "Accepted. No test cases configured.",
                    0,
                    0,
                    0,
                    null);
            }

            var workspacePath = Path.Combine(_workspaceRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workspacePath);

            try
            {
                return normalizedLanguage switch
                {
                    DefaultLanguage => await ExecuteCSharpAsync(problem, normalizedExecutionMode, code, orderedTests, workspacePath),
                    "python" => await ExecutePythonAsync(problem, normalizedExecutionMode, code, orderedTests, workspacePath),
                    "cpp" => await ExecuteCppAsync(problem, normalizedExecutionMode, code, orderedTests, workspacePath),
                    _ => new ExecutionResult(
                        SubmissionStatus.CompilationError,
                        "Unsupported language. Supported languages are C#, Python, and C++.",
                        0,
                        orderedTests.Count,
                        null,
                        null)
                };
            }
            finally
            {
                try
                {
                    if (Directory.Exists(workspacePath))
                        Directory.Delete(workspacePath, recursive: true);
                }
                catch
                {
                    // Best-effort cleanup; stale temp folders are less harmful than failing the request.
                }
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

        private static async Task<ExecutionResult> ExecuteCSharpAsync(
            Problem problem,
            string executionMode,
            string code,
            IReadOnlyList<TestCase> orderedTests,
            string workspacePath)
        {
            var sourceCodeResult = BuildSourceCode(problem, executionMode, DefaultLanguage, code);
            if (sourceCodeResult.ErrorMessage is not null)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    sourceCodeResult.ErrorMessage,
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            await File.WriteAllTextAsync(
                Path.Combine(workspacePath, "SubmissionRunner.csproj"),
                SubmissionProjectTemplate);
            await File.WriteAllTextAsync(
                Path.Combine(workspacePath, "Program.cs"),
                sourceCodeResult.SourceCode);

            var restoreResult = await RunProcessAsync(
                "dotnet",
                ["restore", "--nologo", "--ignore-failed-sources"],
                workspacePath,
                null,
                RestoreTimeout);

            if (restoreResult.TimedOut)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    "Dependency restore timed out.",
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            if (restoreResult.ExitCode != 0)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    BuildOutputMessage(restoreResult),
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            var buildResult = await RunProcessAsync(
                "dotnet",
                ["build", "--no-restore", "--nologo", "--verbosity", "quiet"],
                workspacePath,
                null,
                BuildTimeout);

            if (buildResult.TimedOut)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    "Compilation timed out.",
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            if (buildResult.ExitCode != 0)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    BuildOutputMessage(buildResult),
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            var assemblyPath = Path.Combine(
                workspacePath,
                "bin",
                "Debug",
                "net10.0",
                "SubmissionRunner.dll");

            return await RunAgainstTestsAsync(
                "dotnet",
                [assemblyPath],
                workspacePath,
                orderedTests);
        }

        private static async Task<ExecutionResult> ExecutePythonAsync(
            Problem problem,
            string executionMode,
            string code,
            IReadOnlyList<TestCase> orderedTests,
            string workspacePath)
        {
            var pythonCommand = ResolveAvailableCommand("python3", "python");

            if (pythonCommand is null)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    "Python is not available on the server.",
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            var sourceCodeResult = BuildSourceCode(problem, executionMode, "python", code);
            if (sourceCodeResult.ErrorMessage is not null)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    sourceCodeResult.ErrorMessage,
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            var scriptPath = Path.Combine(workspacePath, "main.py");
            await File.WriteAllTextAsync(scriptPath, sourceCodeResult.SourceCode);

            var syntaxCheckResult = await RunProcessAsync(
                pythonCommand,
                ["-m", "py_compile", scriptPath],
                workspacePath,
                null,
                BuildTimeout);

            if (syntaxCheckResult.TimedOut)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    "Compilation timed out.",
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            if (syntaxCheckResult.ExitCode != 0)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    BuildOutputMessage(syntaxCheckResult),
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            return await RunAgainstTestsAsync(
                pythonCommand,
                [scriptPath],
                workspacePath,
                orderedTests);
        }

        private static async Task<ExecutionResult> ExecuteCppAsync(
            Problem problem,
            string executionMode,
            string code,
            IReadOnlyList<TestCase> orderedTests,
            string workspacePath)
        {
            var compilerCommand = ResolveAvailableCommand("c++", "clang++", "g++");

            if (compilerCommand is null)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    "A C++ compiler is not available on the server.",
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            var sourceCodeResult = BuildSourceCode(problem, executionMode, "cpp", code);
            if (sourceCodeResult.ErrorMessage is not null)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    sourceCodeResult.ErrorMessage,
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            var sourcePath = Path.Combine(workspacePath, "main.cpp");
            var binaryPath = Path.Combine(workspacePath, "SubmissionRunner");

            await File.WriteAllTextAsync(sourcePath, sourceCodeResult.SourceCode);

            var buildResult = await RunProcessAsync(
                compilerCommand,
                ["-std=c++17", "-O2", sourcePath, "-o", binaryPath],
                workspacePath,
                null,
                BuildTimeout);

            if (buildResult.TimedOut)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    "Compilation timed out.",
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            if (buildResult.ExitCode != 0)
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    BuildOutputMessage(buildResult),
                    0,
                    orderedTests.Count,
                    null,
                    null);
            }

            return await RunAgainstTestsAsync(
                binaryPath,
                [],
                workspacePath,
                orderedTests);
        }

        private static async Task<ExecutionResult> RunAgainstTestsAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            string workingDirectory,
            IReadOnlyList<TestCase> orderedTests)
        {
            var totalExecutionTimeMs = 0;
            var passedTests = 0;

            foreach (var testCase in orderedTests)
            {
                var executionResult = await RunProcessAsync(
                    fileName,
                    arguments,
                    workingDirectory,
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
            string workingDirectory,
            string? standardInput,
            TimeSpan timeout)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (!string.IsNullOrEmpty(standardInput))
                await process.StandardInput.WriteAsync(standardInput);

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
                return new ProcessExecutionResult(
                    ExitCode: null,
                    StandardOutput: await outputTask,
                    StandardError: await errorTask,
                    TimedOut: true,
                    ElapsedMilliseconds: (int)stopwatch.ElapsedMilliseconds);
            }

            stopwatch.Stop();

            return new ProcessExecutionResult(
                ExitCode: process.ExitCode,
                StandardOutput: await outputTask,
                StandardError: await errorTask,
                TimedOut: false,
                ElapsedMilliseconds: (int)stopwatch.ElapsedMilliseconds);
        }

        private static void TryKillProcess(Process process)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }

        private static string BuildOutputMessage(ProcessExecutionResult result)
        {
            var message = string.IsNullOrWhiteSpace(result.StandardError)
                ? result.StandardOutput
                : result.StandardError;

            return string.IsNullOrWhiteSpace(message)
                ? "The submission did not produce any diagnostic output."
                : message.Trim();
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

            return new SourceCodeBuildResult(
                harnessTemplate.Replace("{{candidate_code}}", candidateCode, StringComparison.Ordinal),
                null);
        }

        private static string? ResolveAvailableCommand(params string[] commandNames)
        {
            var pathValue = Environment.GetEnvironmentVariable("PATH");

            if (string.IsNullOrWhiteSpace(pathValue))
                return null;

            var pathEntries = pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            foreach (var commandName in commandNames)
            {
                if (Path.IsPathRooted(commandName) && File.Exists(commandName))
                    return commandName;

                foreach (var pathEntry in pathEntries)
                {
                    var fullPath = Path.Combine(pathEntry, commandName);

                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }

            return null;
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

        private sealed record ProcessExecutionResult(
            int? ExitCode,
            string StandardOutput,
            string StandardError,
            bool TimedOut,
            int ElapsedMilliseconds);

        private sealed record SourceCodeBuildResult(
            string SourceCode,
            string? ErrorMessage);
    }
}
