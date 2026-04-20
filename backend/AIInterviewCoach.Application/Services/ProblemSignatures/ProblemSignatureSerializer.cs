using System.Text.Json;
using AIInterviewCoach.Application.DTOs.Problems;

namespace AIInterviewCoach.Application.Services.ProblemSignatures
{
    public static class ProblemSignatureSerializer
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        public static string Serialize(ProblemSignatureDefinitionDto signature)
        {
            return JsonSerializer.Serialize(signature, SerializerOptions);
        }

        public static ProblemSignatureDefinitionDto? Deserialize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return JsonSerializer.Deserialize<ProblemSignatureDefinitionDto>(value, SerializerOptions);
        }
    }
}
