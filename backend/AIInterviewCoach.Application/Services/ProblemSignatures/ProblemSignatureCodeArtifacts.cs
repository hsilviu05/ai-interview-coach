namespace AIInterviewCoach.Application.Services.ProblemSignatures
{
    public sealed record ProblemSignatureCodeArtifacts(
        string CsharpStarterCode,
        string PythonStarterCode,
        string CppStarterCode,
        string CsharpHarnessCode,
        string PythonHarnessCode,
        string CppHarnessCode);
}
