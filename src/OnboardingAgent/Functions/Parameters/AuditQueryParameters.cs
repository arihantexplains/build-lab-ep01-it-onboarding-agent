using System;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// { "source": "graph" (Entra directory audits for userId, the default) or "log" (this tool's ab_adminactionlog),
    ///   "userId", "actionName", "requestedBy", "days": 1-30 default 7, "top": 1-200 default 50 }
    /// </summary>
    public sealed class AuditQueryParameters
    {
        public bool FromActionLog { get; private set; }

        public string UserId { get; private set; }

        public string ActionName { get; private set; }

        public string RequestedBy { get; private set; }

        public int Days { get; private set; }

        public int Top { get; private set; }

        public static AuditQueryParameters Parse(JsonObject p)
        {
            var source = p.GetTrimmed("source") ?? "graph";
            if (!source.Equals("graph", StringComparison.OrdinalIgnoreCase) && !source.Equals("log", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Parameter 'source' must be 'graph' or 'log'.");
            }

            var result = new AuditQueryParameters
            {
                FromActionLog = source.Equals("log", StringComparison.OrdinalIgnoreCase),
                UserId = p.GetTrimmed("userId"),
                ActionName = p.GetTrimmed("actionName"),
                RequestedBy = p.GetTrimmed("requestedBy"),
                Days = p.GetInt("days", 7, 1, 30),
                Top = p.GetInt("top", 50, 1, 200)
            };

            if (!result.FromActionLog && result.UserId == null)
            {
                throw new ValidationException("Parameter 'userId' is required when source is 'graph'.");
            }

            return result;
        }
    }
}
