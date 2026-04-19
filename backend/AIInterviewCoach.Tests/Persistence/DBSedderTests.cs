using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Infrastructure.Persistence;
using AIInterviewCoach.Tests.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AIInterviewCoach.Tests.Persistence
{
    public class DBSedderTests
    {
        [Fact]
        public async Task SeedAsync_ShouldCreateDemoUsersAndStatistic_InDevelopment()
        {
            using var db = TestDbContextFactory.CreateContext();
            var configuration = BuildConfiguration();
            var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };

            await DBSedder.SeedAsync(db, configuration, environment);

            var users = db.Users.OrderBy(user => user.Email).ToList();

            Assert.Equal(2, users.Count);
            Assert.Contains(users, user => user.Email == "candidate@test.com");
            Assert.Contains(users, user => user.Email == "recruiter@test.com");

            var candidate = users.Single(user => user.Email == "candidate@test.com");
            var statistic = db.CandidateStatistics.Single();

            Assert.Equal(candidate.Id, statistic.CandidateId);
            Assert.Equal(0, statistic.TotalSubmissions);
        }

        [Fact]
        public async Task SeedAsync_ShouldSkipDevelopmentSeeding_InProduction()
        {
            using var db = TestDbContextFactory.CreateContext();
            var configuration = BuildConfiguration();
            var environment = new TestHostEnvironment { EnvironmentName = Environments.Production };

            await DBSedder.SeedAsync(db, configuration, environment);

            Assert.Empty(db.Users.ToList());
            Assert.Empty(db.CandidateStatistics.ToList());
        }

        [Fact]
        public void EnsureSafeStartupConfiguration_ShouldThrow_WhenBootstrapAdminIsConfiguredOutsideDevelopment()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["BootstrapAdmin:Email"] = "admin@test.com",
                ["BootstrapAdmin:Password"] = "Password123!"
            });

            var environment = new TestHostEnvironment { EnvironmentName = Environments.Production };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                DBSedder.EnsureSafeStartupConfiguration(configuration, environment));

            Assert.Contains("Development environment", exception.Message);
        }

        [Fact]
        public async Task SeedAsync_ShouldCreateConfiguredAdmin_InDevelopment()
        {
            using var db = TestDbContextFactory.CreateContext();
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["BootstrapAdmin:Email"] = "admin@test.com",
                ["BootstrapAdmin:Password"] = "Password123!",
                ["BootstrapAdmin:FullName"] = "Admin Demo"
            });

            var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };

            await DBSedder.SeedAsync(db, configuration, environment);

            var admin = db.Users.Single(user => user.Email == "admin@test.com");
            Assert.Equal("Admin Demo", admin.FullName);
            Assert.Equal(Domain.Enums.UserRole.Admin, admin.UserRole);
        }

        [Fact]
        public async Task SeedAsync_ShouldUpgradeLegacyStarterProblemPythonTemplate_InDevelopment()
        {
            using var db = TestDbContextFactory.CreateContext();
            var configuration = BuildConfiguration();
            var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };
            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, title: "Two Sum");
            problem.ExecutionMode = ProblemExecutionModes.FunctionSignature;
            problem.CsharpStarterCode = """
                public class Solution
                {
                    public int[] TwoSum(int[] nums, int target)
                    {
                        
                    }
                }
                """;
            problem.PythonStarterCode = """
                from typing import List


                class Solution:
                    def twoSum(self, nums: List[int], target: int) -> List[int]:
                        
                """;
            problem.CppStarterCode = """
                #include <vector>
                using namespace std;

                class Solution {
                public:
                    vector<int> twoSum(vector<int>& nums, int target) {
                        
                    }
                };
                """;
            db.SaveChanges();

            await DBSedder.SeedAsync(db, configuration, environment);

            var updatedProblem = db.Problems.Single(existingProblem => existingProblem.Id == problem.Id);
            Assert.Contains("return []", updatedProblem.PythonStarterCode);
            Assert.Contains("new int[0]", updatedProblem.CsharpStarterCode);
            Assert.Contains("return {};", updatedProblem.CppStarterCode);
            Assert.DoesNotContain("Use JSON input like", updatedProblem.ConstraintsText, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("nums = [2,7,11,15], target = 9", updatedProblem.ExampleInput);
        }

        private static IConfiguration BuildConfiguration(Dictionary<string, string?>? values = null)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
                .Build();
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Development;
            public string ApplicationName { get; set; } = "AIInterviewCoach.Tests";
            public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        }
    }
}
