namespace AIInterviewCoach.Application.Services
{
    internal static class StarterProblemCatalogFactory
    {
        public static IReadOnlyList<StarterProblemSeed> Build(Guid createdByUserId)
        {
            return ProblemTemplateCatalog.BuildStarterProblemSeeds(createdByUserId);
        }
    }
}
