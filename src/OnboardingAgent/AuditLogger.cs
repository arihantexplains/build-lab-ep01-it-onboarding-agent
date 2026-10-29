using System;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace OnboardingAgent
{
    /// <summary>
    /// Writes one ab_adminactionlog row per call, success or failure.
    /// </summary>
    public sealed class AuditLogger
    {
        public const string TableName = "ab_adminactionlog";

        private static readonly string[] SensitiveKeyFragments = { "password", "secret", "token", "credential" };

        private readonly IOrganizationService _service;
        private readonly ITracingService _trace;

        public AuditLogger(IOrganizationService service, ITracingService trace)
        {
            _service = service;
            _trace = trace;
        }

        public bool TryWrite(string actionName, string requestedBy, string approvalId, string correlationId, string parametersJson,
            bool success, string message, string resultJson, TimeSpan duration)
        {
            var row = new Entity(TableName);
            row["ab_name"] = Truncate((actionName ?? "(none)") + " " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "Z", 200);
            row["ab_actionname"] = Truncate(actionName, 100);
            row["ab_requestedby"] = Truncate(requestedBy, 200);
            row["ab_approvalid"] = Truncate(approvalId, 100);
            row["ab_correlationid"] = correlationId;
            row["ab_success"] = success;
            row["ab_message"] = Truncate(message, 4000);
            // Lengths match the ab_adminactionlog columns; a value too long for its column would fail the write and roll back the call.
            row["ab_target"] = Truncate(Target(parametersJson), 300);
            row["ab_parametersjson"] = Truncate(Redact(parametersJson), 4000);
            row["ab_durationms"] = (int)Math.Min(int.MaxValue, duration.TotalMilliseconds);

            try
            {
                _service.Create(row);
                return true;
            }
            catch (Exception ex)
            {
                // A failed create inside the plug-in transaction may still roll back the whole call; nothing more can be done here.
                _trace.Trace("Audit log write failed: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>The user or group acted on, taken from the usual parameter names.</summary>
        private static string Target(string parametersJson)
        {
            try
            {
                var p = Json.ParseObject(parametersJson);
                foreach (var key in new[] { "userId", "targetUserId", "displayName", "group", "groupId", "name", "department", "jobTitle" })
                {
                    var value = p.GetTrimmed(key);
                    if (value != null)
                    {
                        return value;
                    }
                }
            }
            catch (Exception)
            {
                // The target column is a convenience; a malformed request is still logged.
            }

            return null;
        }

        /// <summary>No action accepts a password, but redacting defensively keeps one out of the log if a caller sends it anyway.</summary>
        private static string Redact(string parametersJson)
        {
            if (string.IsNullOrWhiteSpace(parametersJson))
            {
                return null;
            }

            try
            {
                var parsed = Json.Parse(parametersJson);
                RedactNode(parsed);
                return Json.Serialize(parsed);
            }
            catch (FormatException)
            {
                return "(not valid JSON; not stored)";
            }
        }

        private static void RedactNode(object node)
        {
            var obj = node as JsonObject;
            if (obj != null)
            {
                foreach (var key in obj.Keys.ToList())
                {
                    if (SensitiveKeyFragments.Any(fragment => key.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        obj[key] = "***";
                    }
                    else
                    {
                        RedactNode(obj[key]);
                    }
                }

                return;
            }

            var array = node as JsonArray;
            if (array != null)
            {
                foreach (var item in array)
                {
                    RedactNode(item);
                }
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            return value == null || value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
