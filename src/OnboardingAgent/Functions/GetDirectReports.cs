using System;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// The people who report directly to a user. Reports outside the managed scope are counted but not named.
    /// </summary>
    public static class GetDirectReports
    {
        private const int MaxReports = 500;

        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = UserTargetParameters.Parse(parameters);
            var manager = ctx.Validator.ReadableUser(p.UserId);

            bool truncated;
            var all = ctx.Graph.GetAll("/users/" + manager.Id + "/directReports/microsoft.graph.user?$select=" + DirectoryService.UserSelect,
                    MaxReports, out truncated)
                .Select(UserInfo.From)
                .ToList();
            var inScope = all
                .Where(u => ctx.Validator.InScope(u.DisplayName))
                .OrderBy(u => u.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var outOfScope = all.Count - inScope.Count;

            var message = inScope.Count == 0
                ? "Nobody in the managed scope reports to " + manager.DisplayName + "."
                : inScope.Count + " people report to " + manager.DisplayName + ": " + string.Join(", ", inScope.Select(u => u.DisplayName)) + ".";
            if (outOfScope > 0)
            {
                message += " " + outOfScope + " other direct report(s) are outside the managed scope.";
            }

            return ActionResult.Ok(message, new JsonObject
            {
                { "manager", manager.ToJson() },
                { "directReports", new JsonArray(inScope.Select(u => u.ToJson())) },
                { "outOfScopeCount", outOfScope },
                { "truncated", truncated }
            });
        }
    }
}
