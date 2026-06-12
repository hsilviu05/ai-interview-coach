using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Application.Services.ProblemSignatures;

namespace AIInterviewCoach.Tests.Services
{
    public class ProblemTemplateServiceTests
    {
        [Fact]
        public void GetSignaturePreview_ShouldReturnStarterCodeDerivedFromSignature()
        {
            var service = new ProblemTemplateService();
            var signature = new ProblemSignatureDefinitionDto
            {
                InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                CsharpMethodName = "TwoSum",
                PythonMethodName = "twoSum",
                CppMethodName = "twoSum",
                ReturnType = ProblemSignatureTypeKeys.IntArray,
                Parameters =
                [
                    new ProblemSignatureParameterDto { Name = "nums", Type = ProblemSignatureTypeKeys.IntArray },
                    new ProblemSignatureParameterDto { Name = "target", Type = ProblemSignatureTypeKeys.Int }
                ]
            };

            var result = service.GetSignaturePreview(signature);

            Assert.Contains("TwoSum", result.CsharpStarterCode);
            Assert.Contains("twoSum", result.PythonStarterCode);
            Assert.Contains("twoSum", result.CppStarterCode);
        }
    }
}
