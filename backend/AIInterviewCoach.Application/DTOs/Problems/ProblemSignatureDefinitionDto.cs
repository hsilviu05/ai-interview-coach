namespace AIInterviewCoach.Application.DTOs.Problems
{
    public class ProblemSignatureDefinitionDto
    {
        public string InputBindingMode { get; set; } = "json_object";
        public string CsharpMethodName { get; set; } = string.Empty;
        public string PythonMethodName { get; set; } = string.Empty;
        public string CppMethodName { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public List<ProblemSignatureParameterDto> Parameters { get; set; } = [];
    }

    public class ProblemSignatureParameterDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
