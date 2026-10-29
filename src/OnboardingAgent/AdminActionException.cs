using System;

namespace OnboardingAgent
{
    /// <summary>
    /// An error whose message is safe to return to the caller unchanged.
    /// </summary>
    public class AdminActionException : Exception
    {
        /// <summary>Creates the error with a caller-safe message.</summary>
        /// <param name="message">Text returned to the caller as is.</param>
        public AdminActionException(string message) : base(message)
        {
        }

        /// <summary>Creates the error with a caller-safe message and keeps the original error for tracing.</summary>
        /// <param name="message">Text returned to the caller as is.</param>
        /// <param name="innerException">The underlying error.</param>
        public AdminActionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
