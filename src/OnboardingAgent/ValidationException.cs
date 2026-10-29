namespace OnboardingAgent
{
    /// <summary>
    /// The request was refused before anything was changed.
    /// </summary>
    public sealed class ValidationException : AdminActionException
    {
        public ValidationException(string message) : base(message)
        {
        }
    }
}
