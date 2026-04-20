namespace AIInterviewCoach.Application.Services.ProblemSignatures
{
    public static class ProblemSignatureInputBindingModes
    {
        public const string JsonObject = "json_object";
        public const string RawText = "raw_text";

        public static bool IsSupported(string value)
        {
            return value is JsonObject or RawText;
        }
    }
}
