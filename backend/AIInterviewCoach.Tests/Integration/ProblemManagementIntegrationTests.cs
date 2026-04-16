using System.Net;
using System.Net.Http.Json;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Tests.Common;

namespace AIInterviewCoach.Tests.Integration
{
    public class ProblemManagementIntegrationTests
    {
        [Fact]
        public async Task GetProblemTemplates_ShouldReturnSharedTemplates_ForAdmin()
        {
            await using var factory = new TestApiApplicationFactory();
            using var adminClient = await factory.CreateAuthenticatedClientAsync("admin@test.com");

            var response = await adminClient.GetAsync("/api/problems/templates");

            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

            var templates = await response.Content.ReadFromJsonAsync<List<ProblemTemplateResponseDto>>();

            Assert.NotNull(templates);
            Assert.Contains(templates!, template => template.Key == "two-sum");
            Assert.Contains(templates!, template => template.Key == "generic-function");
        }

        [Fact]
        public async Task GetProblemTemplates_ShouldRejectInterviewerAccess()
        {
            await using var factory = new TestApiApplicationFactory();
            using var interviewerClient = await factory.CreateAuthenticatedClientAsync("recruiter@test.com");

            var response = await interviewerClient.GetAsync("/api/problems/templates");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ReplaceCatalogWithStarterSet_ShouldSeedSharedProblems_EndToEnd()
        {
            await using var factory = new TestApiApplicationFactory();
            using var adminClient = await factory.CreateAuthenticatedClientAsync("admin@test.com");
            using var candidateClient = await factory.CreateAuthenticatedClientAsync("candidate@test.com");

            var replaceResponse = await adminClient.PostAsync("/api/problems/catalog/replace-with-starter-set", null);

            Assert.True(replaceResponse.IsSuccessStatusCode, await replaceResponse.Content.ReadAsStringAsync());

            var problemsResponse = await candidateClient.GetAsync("/api/problems");
            Assert.True(problemsResponse.IsSuccessStatusCode, await problemsResponse.Content.ReadAsStringAsync());

            var problems = await problemsResponse.Content.ReadFromJsonAsync<List<ProblemSummaryResponseDto>>();

            Assert.NotNull(problems);
            Assert.Equal(4, problems!.Count);
            Assert.Contains(problems, problem => problem.Title == "Two Sum");
            Assert.Contains(problems, problem => problem.Title == "Best Time to Buy and Sell Stock");
        }

        [Fact]
        public async Task CreateProblemFromSharedTemplate_ShouldMakePublicProblemAvailableToCandidate()
        {
            await using var factory = new TestApiApplicationFactory();
            using var adminClient = await factory.CreateAuthenticatedClientAsync("admin@test.com");
            using var candidateClient = await factory.CreateAuthenticatedClientAsync("candidate@test.com");

            var templates = await adminClient.GetFromJsonAsync<List<ProblemTemplateResponseDto>>("/api/problems/templates");
            var selectedTemplate = Assert.Single(templates!, template => template.Key == "two-sum");

            var createResponse = await adminClient.PostAsJsonAsync("/api/problems", new CreateProblemRequestDto
            {
                Title = "Two Sum Integration Variant",
                Description = selectedTemplate.Description,
                Difficulty = selectedTemplate.Difficulty,
                Topic = selectedTemplate.Topic,
                ConstraintsText = selectedTemplate.ConstraintsText,
                ExampleInput = selectedTemplate.ExampleInput,
                ExampleOutput = selectedTemplate.ExampleOutput,
                ExecutionMode = selectedTemplate.ExecutionMode,
                CsharpStarterCode = selectedTemplate.CsharpStarterCode,
                PythonStarterCode = selectedTemplate.PythonStarterCode,
                CppStarterCode = selectedTemplate.CppStarterCode,
                CsharpHarnessTemplate = selectedTemplate.CsharpHarnessTemplate,
                PythonHarnessTemplate = selectedTemplate.PythonHarnessTemplate,
                CppHarnessTemplate = selectedTemplate.CppHarnessTemplate,
                IsPublic = true
            });

            Assert.True(createResponse.IsSuccessStatusCode, await createResponse.Content.ReadAsStringAsync());

            var createdProblem = await createResponse.Content.ReadFromJsonAsync<ProblemResponseDto>();
            Assert.NotNull(createdProblem);

            var candidateResponse = await candidateClient.GetAsync($"/api/problems/{createdProblem!.Id}");
            Assert.True(candidateResponse.IsSuccessStatusCode, await candidateResponse.Content.ReadAsStringAsync());

            var candidateProblem = await candidateResponse.Content.ReadFromJsonAsync<ProblemResponseDto>();

            Assert.NotNull(candidateProblem);
            Assert.Equal("Two Sum Integration Variant", candidateProblem!.Title);
            Assert.Equal("function", candidateProblem.ExecutionMode);
            Assert.Contains("def twoSum", candidateProblem.PythonStarterCode);
            Assert.Contains("public int[] TwoSum", candidateProblem.CsharpStarterCode);
        }
    }
}
