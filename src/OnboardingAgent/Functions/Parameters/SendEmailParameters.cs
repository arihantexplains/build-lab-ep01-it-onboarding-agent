using System.Collections.Generic;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "to": [...], "cc": [...], "subject": ..., "body": ..., "isHtml": false by default }</summary>
    public sealed class SendEmailParameters
    {
        private const int MaxRecipients = 20;
        private const int MaxSubjectLength = 255;
        private const int MaxBodyLength = 100000;

        public List<string> To { get; private set; }

        public List<string> Cc { get; private set; }

        public string Subject { get; private set; }

        public string Body { get; private set; }

        public bool IsHtml { get; private set; }

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
