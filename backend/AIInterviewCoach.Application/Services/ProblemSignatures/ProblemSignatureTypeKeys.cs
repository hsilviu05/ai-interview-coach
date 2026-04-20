namespace AIInterviewCoach.Application.Services.ProblemSignatures
{
    public static class ProblemSignatureTypeKeys
    {
        public const string String = "string";
        public const string Int = "int";
        public const string Bool = "bool";
        public const string IntArray = "int_array";
        public const string StringArray = "string_array";

        public static IReadOnlyList<string> SupportedValues { get; } =
        [
            String,
            Int,
            Bool,
            IntArray,
            StringArray
        ];

        public static bool IsSupported(string value)
        {
            return SupportedValues.Contains(value);
        }
    }
}
