namespace OnboardingAgent
{
    /// <summary>
    /// The request was refused before anything was changed.
    /// </summary>
    public sealed class ValidationException : AdminActionException
    {
        /// <summary>Creates the error with the reason the request was refused.</summary>
        /// <param name="message">Text returned to the caller as is.</param>
        public ValidationException(string message) : base(message)
        {
        }
    }
}
