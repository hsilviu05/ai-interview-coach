using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services.ProblemTemplates;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Services.ProblemSignatures
{
    public static class ProblemCodeResolver
    {
        public static ProblemSignatureDefinitionDto? ResolveSignature(Problem problem)
        {
            return ResolveSignature(problem.SignatureDefinitionJson);
        }

        public static ProblemSignatureDefinitionDto? ResolveSignature(string? signatureDefinitionJson)
        {
            return ProblemSignatureSerializer.Deserialize(signatureDefinitionJson);
        }

        public static string ResolveVisibleStarterCode(Problem problem, string language)
        {
            var artifacts = TryGenerateArtifacts(problem);
            if (artifacts is not null)
            {
                return ResolveGeneratedStarterCode(artifacts, language);
            }

            return ProblemTemplateCatalog.ResolveVisibleStarterCode(
                problem.ExecutionMode,
                language,
                ResolveStoredStarterCode(problem, language));
        }

        public static string? ResolveHarnessTemplate(Problem problem, string language)
        {
            var artifacts = TryGenerateArtifacts(problem);
            if (artifacts is not null)
            {
                return ResolveGeneratedHarnessTemplate(artifacts, language);
            }

            if (!string.Equals(
                problem.ExecutionMode,
                ProblemExecutionModes.FunctionSignature,
                StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return language switch
            {
                "csharp" => problem.CsharpHarnessTemplate,
                "python" => problem.PythonHarnessTemplate,
                "cpp" => problem.CppHarnessTemplate,
                _ => null
            };
        }

        private static ProblemSignatureCodeArtifacts? TryGenerateArtifacts(Problem problem)
        {
            var signature = ResolveSignature(problem);

            if (signature is null)
            {
                return null;
            }

            return ProblemSignatureCodeGenerator.Generate(signature);
        }

        private static string ResolveStoredStarterCode(Problem problem, string language)
        {
            return language switch
            {
                "csharp" => problem.CsharpStarterCode,
                "python" => problem.PythonStarterCode,
                "cpp" => problem.CppStarterCode,
                _ => string.Empty
            };
        }

        private static string ResolveGeneratedStarterCode(
            ProblemSignatureCodeArtifacts artifacts,
            string language)
        {
            return language switch
            {
                "csharp" => artifacts.CsharpStarterCode,
                "python" => artifacts.PythonStarterCode,
                "cpp" => artifacts.CppStarterCode,
                _ => string.Empty
            };
        }

        private static string ResolveGeneratedHarnessTemplate(
            ProblemSignatureCodeArtifacts artifacts,
            string language)
        {
            return language switch
            {
                "csharp" => artifacts.CsharpHarnessCode,
                "python" => artifacts.PythonHarnessCode,
                "cpp" => artifacts.CppHarnessCode,
                _ => string.Empty
            };
        }
    }
}
