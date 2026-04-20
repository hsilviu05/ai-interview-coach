namespace AIInterviewCoach.Application.DTOs.Problems
{
    public class ProblemTemplateResponseDto
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string ConstraintsText { get; set; } = string.Empty;
        public string ExampleInput { get; set; } = string.Empty;
        public string ExampleOutput { get; set; } = string.Empty;
        public string ExecutionMode { get; set; } = string.Empty;
        public ProblemSignatureDefinitionDto? Signature { get; set; }
        public string CsharpStarterCode { get; set; } = string.Empty;
        public string PythonStarterCode { get; set; } = string.Empty;
        public string CppStarterCode { get; set; } = string.Empty;
    }
}
