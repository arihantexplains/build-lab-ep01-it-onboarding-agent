using System.Collections.Generic;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "to": [...], "cc": [...], "subject": ..., "body": ..., "isHtml": false by default }</summary>
    public sealed class SendEmailParameters
    {
        private const int MaxRecipients = 20;
        private const int MaxSubjectLength = 255;
        private const int MaxBodyLength = 100000;

        /// <summary>Recipient addresses from "to". Required, at least one. To and Cc together may hold at most 20 addresses.</summary>
        public List<string> To { get; private set; }

        /// <summary>Copy addresses from "cc". Optional; counts toward the limit of 20 recipients.</summary>
        public List<string> Cc { get; private set; }

        /// <summary>Subject line from "subject". Required, 255 characters or fewer.</summary>
        public string Subject { get; private set; }

        /// <summary>Message body from "body". Required, 100000 characters or fewer.</summary>
        public string Body { get; private set; }

        /// <summary>Whether the body is HTML rather than plain text, from "isHtml". Optional, default false.</summary>
        public bool IsHtml { get; private set; }

        /// <summary>Reads and checks the parameters for SendEmail.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">A required value is missing, there are no or too many recipients, or the subject or body is too long.</exception>
        public static SendEmailParameters Parse(JsonObject p)
        {
            var result = new SendEmailParameters
            {
                To = p.GetStringList("to"),
                Cc = p.GetStringList("cc"),
                Subject = p.RequireString("subject"),
                Body = p.RequireString("body"),
                IsHtml = p.GetBool("isHtml", false)
            };

            if (result.To.Count == 0)
            {
                throw new ValidationException("Parameter 'to' must list at least one recipient.");
            }

            if (result.To.Count + result.Cc.Count > MaxRecipients)
            {
                throw new ValidationException("At most " + MaxRecipients + " recipients are allowed.");
            }

            if (result.Subject.Length > MaxSubjectLength)
            {
                throw new ValidationException("subject must be " + MaxSubjectLength + " characters or fewer.");
            }

            if (result.Body.Length > MaxBodyLength)
            {
                throw new ValidationException("body must be " + MaxBodyLength + " characters or fewer.");
            }

            return result;
        }
    }
}
