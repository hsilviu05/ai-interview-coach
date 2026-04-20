using System.Text.RegularExpressions;
using AIInterviewCoach.Application.DTOs.Problems;

namespace AIInterviewCoach.Application.Services.ProblemSignatures
{
    public static class ProblemSignatureValidation
    {
        private const int MaxParameterCount = 8;
        private static readonly Regex IdentifierPattern = new(
            "^[A-Za-z_][A-Za-z0-9_]*$",
            RegexOptions.Compiled);

        public static ProblemSignatureDefinitionDto ValidateAndNormalize(
            ProblemSignatureDefinitionDto? signature)
        {
            if (signature is null)
            {
                throw new InvalidOperationException(
                    "Function-signature problems require structured signature metadata.");
            }

            var normalizedInputBindingMode = NormalizeRequired(signature.InputBindingMode);
            if (!ProblemSignatureInputBindingModes.IsSupported(normalizedInputBindingMode))
            {
                throw new InvalidOperationException(
                    $"Unsupported input binding mode '{signature.InputBindingMode}'.");
            }

            var normalizedReturnType = NormalizeRequired(signature.ReturnType);
            if (!ProblemSignatureTypeKeys.IsSupported(normalizedReturnType))
            {
                throw new InvalidOperationException(
                    $"Unsupported return type '{signature.ReturnType}'.");
            }

            var normalizedParameters = (signature.Parameters ?? [])
                .Select(parameter => new ProblemSignatureParameterDto
                {
                    Name = NormalizeRequired(parameter.Name),
                    Type = NormalizeRequired(parameter.Type)
                })
                .ToList();

            if (normalizedParameters.Count > MaxParameterCount)
            {
                throw new InvalidOperationException(
                    $"Function signatures support up to {MaxParameterCount} parameters.");
            }

            var duplicateNames = normalizedParameters
                .GroupBy(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicateNames is not null)
            {
                throw new InvalidOperationException(
                    $"Parameter '{duplicateNames.Key}' is declared more than once.");
            }

            foreach (var parameter in normalizedParameters)
            {
                EnsureIdentifier(parameter.Name, "parameter");

                if (!ProblemSignatureTypeKeys.IsSupported(parameter.Type))
                {
                    throw new InvalidOperationException(
                        $"Unsupported parameter type '{parameter.Type}' for '{parameter.Name}'.");
                }
            }

            var normalized = new ProblemSignatureDefinitionDto
            {
                InputBindingMode = normalizedInputBindingMode,
                CsharpMethodName = NormalizeRequired(signature.CsharpMethodName),
                PythonMethodName = NormalizeRequired(signature.PythonMethodName),
                CppMethodName = NormalizeRequired(signature.CppMethodName),
                ReturnType = normalizedReturnType,
                Parameters = normalizedParameters
            };

            EnsureIdentifier(normalized.CsharpMethodName, "C# method");
            EnsureIdentifier(normalized.PythonMethodName, "Python method");
            EnsureIdentifier(normalized.CppMethodName, "C++ method");

            if (normalized.InputBindingMode == ProblemSignatureInputBindingModes.RawText)
            {
                if (normalized.Parameters.Count != 1 || normalized.Parameters[0].Type != ProblemSignatureTypeKeys.String)
                {
                    throw new InvalidOperationException(
                        "Raw-text signatures must define exactly one string parameter.");
                }
            }

            return normalized;
        }

        private static void EnsureIdentifier(string value, string label)
        {
            if (!IdentifierPattern.IsMatch(value))
            {
                throw new InvalidOperationException(
                    $"{label} name '{value}' is invalid. Use letters, digits, and underscores only.");
            }
        }

        private static string NormalizeRequired(string value)
        {
            var trimmed = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                throw new InvalidOperationException("Signature metadata contains a required blank value.");
            }

            return trimmed;
        }
    }
}
