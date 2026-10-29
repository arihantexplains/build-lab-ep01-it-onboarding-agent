using System;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// { "source": "graph" (Entra directory audits for userId, the default) or "log" (this tool's ab_adminactionlog),
    ///   "userId", "actionName", "requestedBy", "days": 1-30 default 7, "top": 1-200 default 50 }
    /// </summary>
    public sealed class AuditQueryParameters
    {
        /// <summary>True when "source" is "log" (this tool's action log table), false when it is "graph" (Entra directory audits). Optional, default "graph", any letter case.</summary>
        public bool FromActionLog { get; private set; }

        /// <summary>The user whose directory audits are read, by object ID or UPN, from "userId". Required when the source is "graph"; not used for "log".</summary>
        public string UserId { get; private set; }

        /// <summary>Exact action name to filter the action log by, from "actionName". Optional; used only when the source is "log".</summary>
        public string ActionName { get; private set; }

        /// <summary>Exact requester to filter the action log by, from "requestedBy". Optional; used only when the source is "log".</summary>
        public string RequestedBy { get; private set; }

        /// <summary>How many days back to look, from "days". Optional, 1 to 30, default 7.</summary>
        public int Days { get; private set; }

        /// <summary>Maximum number of entries to return, from "top". Optional, 1 to 200, default 50.</summary>
        public int Top { get; private set; }

        /// <summary>Reads and checks the parameters for AuditQuery.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"source" is not "graph" or "log", "userId" is missing for the graph source, or a number is out of range.</exception>
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
