using AIInterviewCoach.Application.Services;

namespace AIInterviewCoach.Tests.Services
{
    public class ProblemTemplateCatalogTests
    {
        [Fact]
        public void GetCreateProblemTemplates_ShouldReturnUniqueTemplateKeys()
        {
            var templates = ProblemTemplateCatalog.GetCreateProblemTemplates();
            var uniqueKeys = templates
                .Select(template => template.Key)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            Assert.Equal(templates.Count, uniqueKeys.Count);
        }

        [Fact]
        public void GetCreateProblemTemplates_ShouldKeepExpectedSharedTemplateOrder()
        {
            var templates = ProblemTemplateCatalog.GetCreateProblemTemplates();

            Assert.Equal(
                [
                    "generic-function",
                    "two-sum",
                    "valid-parentheses",
                    "merge-strings-alternately",
                    "best-time-to-buy-sell-stock"
                ],
                templates.Select(template => template.Key));
        }

        [Fact]
        public void BuiltInJudgeTemplates_ShouldExposeStatementStyleExamplesInsteadOfJsonPrompts()
        {
            var templates = ProblemTemplateCatalog.GetCreateProblemTemplates()
                .Where(template => template.Key != "generic-function")
                .ToList();

            Assert.All(templates, template =>
            {
                Assert.Equal("function", template.ExecutionMode);
                Assert.DoesNotContain("JSON", template.ConstraintsText, StringComparison.OrdinalIgnoreCase);
                Assert.False(template.ExampleInput.TrimStart().StartsWith('{'));
            });
        }
    }
}
