using System.Diagnostics;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Infrastructure.Services
{
    public sealed class DotnetCodeExecutor : ICodeExecutor
    {
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
            string code,
            string language,
            IEnumerable<TestCase> testCases)
        {
            var orderedTests = testCases
                .OrderBy(testCase => testCase.OrderIndex)
                .ToList();

            if (!string.Equals(language.Trim(), "csharp", StringComparison.OrdinalIgnoreCase))
            {
                return new ExecutionResult(
                    SubmissionStatus.CompilationError,
                    "Only C# submissions are currently supported.",
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
                await File.WriteAllTextAsync(
                    Path.Combine(workspacePath, "SubmissionRunner.csproj"),
                    SubmissionProjectTemplate);
                await File.WriteAllTextAsync(
                    Path.Combine(workspacePath, "Program.cs"),
                    code);

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

                var totalExecutionTimeMs = 0;
                var passedTests = 0;

                foreach (var testCase in orderedTests)
                {
                    var executionResult = await RunProcessAsync(
                        "dotnet",
                        [assemblyPath],
                        workspacePath,
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

        private sealed record ProcessExecutionResult(
            int? ExitCode,
            string StandardOutput,
            string StandardError,
            bool TimedOut,
            int ElapsedMilliseconds);
    }
}
