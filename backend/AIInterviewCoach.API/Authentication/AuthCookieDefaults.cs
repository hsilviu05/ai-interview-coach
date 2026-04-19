namespace AIInterviewCoach.API.Authentication
{
    public static class AuthCookieDefaults
    {
        public const string CookieName = "aic_auth";
        public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);
    }
}
