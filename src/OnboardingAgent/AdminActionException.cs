using System;

namespace OnboardingAgent
{
    /// <summary>
    /// An error whose message is safe to return to the caller unchanged.
    /// </summary>
    public class AdminActionException : Exception
    {
        public AdminActionException(string message) : base(message)
        {
        }

        public AdminActionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
