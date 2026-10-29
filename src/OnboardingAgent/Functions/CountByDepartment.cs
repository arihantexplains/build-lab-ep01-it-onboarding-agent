using System;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Counts in-scope users per department. Users are paged and counted here rather than with $count, because Graph's
    /// startswith is case-insensitive and the prefix rule is not.
    /// </summary>
    public static class CountByDepartment
    {
        private const int MaxUsers = 20000;

        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = CountByDepartmentParameters.Parse(parameters);

            var filter = "startswith(displayName," + DirectoryService.Literal(ctx.Config.RequiredNamePrefix) + ")";
            if (p.Department != null)
            {
                filter += " and department eq " + DirectoryService.Literal(p.Department);
            }

            if (!p.IncludeDisabled)
            {
                filter += " and accountEnabled eq true";
            }

            bool truncated;
            var users = ctx.Graph.GetAll("/users?" + DirectoryService.Filter(filter) + "&$count=true&$select=displayName,department&$top=999",
                MaxUsers, out truncated, new GraphCallOptions { EventualConsistency = true });

            var counts = users
                .Where(u => ctx.Validator.InScope(u.GetString("displayName")))
                .GroupBy(u => u.GetTrimmed("department") ?? "(none)", StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Department, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var total = counts.Sum(x => x.Count);

            var data = new JsonObject
            {
                { "total", total },
                { "departments", new JsonArray(counts.Select(x => new JsonObject { { "department", x.Department }, { "count", x.Count } })) },
                { "truncated", truncated }
            };

            var message = total + " user(s) in " + counts.Count + " department(s)" + (truncated ? " (partial count: the user limit or time limit was reached)." : ".");
            return ActionResult.Ok(message, data);
        }
    }
}
