using System;
using System.Globalization;
using System.Linq;
using Microsoft.Xrm.Sdk.Query;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Entra ID directory audit events for a user, or this tool's own action log.
    /// </summary>
    public static class AuditQuery
    {
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = AuditQueryParameters.Parse(parameters);
            return p.FromActionLog ? QueryActionLog(ctx, p) : QueryDirectoryAudits(ctx, p);
        }

        private static ActionResult QueryDirectoryAudits(ActionContext ctx, AuditQueryParameters p)
        {
            var user = ctx.Validator.ReadableUser(p.UserId);
            var since = DateTime.UtcNow.AddDays(-p.Days).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            var filter = "activityDateTime ge " + since + " and targetResources/any(t:t/id eq " + DirectoryService.Literal(user.Id) + ")";

            var events = new JsonArray();
            foreach (var entry in ctx.Graph.Get("/auditLogs/directoryAudits?" + DirectoryService.Filter(filter) + "&$top=" + p.Top)
                .GetArray("value").Objects())
            {
                var initiatedBy = entry.GetObject("initiatedBy");
                events.Add(new JsonObject
                {
                    { "time", entry.GetString("activityDateTime") },
                    { "activity", entry.GetString("activityDisplayName") },
                    { "category", entry.GetString("category") },
                    { "result", entry.GetString("result") },
                    { "resultReason", entry.GetString("resultReason") },
                    {
                        "initiatedBy", initiatedBy?.GetObject("user")?.GetString("userPrincipalName")
                            ?? initiatedBy?.GetObject("app")?.GetString("displayName")
                    },
                    {
                        "targets", new JsonArray(entry.GetArray("targetResources").Objects()
                            .Select(t => t.GetString("userPrincipalName") ?? t.GetString("displayName") ?? t.GetString("id")))
                    }
                });
            }

            return ActionResult.Ok(events.Count + " directory audit event(s) for " + user.DisplayName + " in the last " + p.Days + " day(s).", new JsonObject
            {
                { "user", user.ToJson() },
                { "events", events }
            });
        }

        private static ActionResult QueryActionLog(ActionContext ctx, AuditQueryParameters p)
        {
            var query = new QueryExpression(AuditLogger.TableName)
            {
                ColumnSet = new ColumnSet("ab_actionname", "ab_requestedby", "ab_approvalid", "ab_correlationid", "ab_success",
                    "ab_message", "ab_durationms", "createdon"),
                TopCount = p.Top
            };
            query.Criteria.AddCondition("createdon", ConditionOperator.GreaterEqual, DateTime.UtcNow.AddDays(-p.Days));
            if (p.ActionName != null)
            {
                query.Criteria.AddCondition("ab_actionname", ConditionOperator.Equal, p.ActionName);
            }

            if (p.RequestedBy != null)
            {
                query.Criteria.AddCondition("ab_requestedby", ConditionOperator.Equal, p.RequestedBy);
            }

            query.AddOrder("createdon", OrderType.Descending);

            var rows = new JsonArray(ctx.Service.RetrieveMultiple(query).Entities.Select(row => new JsonObject
            {
                { "time", row.GetAttributeValue<DateTime?>("createdon") },
                { "action", row.GetAttributeValue<string>("ab_actionname") },
                { "requestedBy", row.GetAttributeValue<string>("ab_requestedby") },
                { "approvalId", row.GetAttributeValue<string>("ab_approvalid") },
                { "correlationId", row.GetAttributeValue<string>("ab_correlationid") },
                { "success", row.GetAttributeValue<bool?>("ab_success") },
                { "message", row.GetAttributeValue<string>("ab_message") },
                { "durationMs", row.GetAttributeValue<int?>("ab_durationms") }
            }));

            return ActionResult.Ok(rows.Count + " action log entr" + (rows.Count == 1 ? "y" : "ies") + " in the last " + p.Days + " day(s).",
                new JsonObject { { "entries", rows } });
        }
    }
}
