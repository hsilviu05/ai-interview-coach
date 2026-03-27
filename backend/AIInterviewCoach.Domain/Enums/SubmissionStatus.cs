namespace AIInterviewCoach.Domain.Enums
{
    public enum SubmissionStatus
    {
        Pending = 0,
        Accepted = 1,
        WrongAnswer = 2,
        RuntimeError = 3,
        TimeLimitExceeded = 4,
        CompilationError = 5
    }
}