using System;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Finds managed users by part of the name, department and/or part of the job title. Only users with the required
    /// prefix are searched, so nothing outside the managed scope is ever returned.
    /// </summary>
    public static class SearchUsers
    {
        private const int MaxScanned = 5000;

        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = SearchUsersParameters.Parse(parameters);

            var filter = "startswith(displayName," + DirectoryService.Literal(ctx.Config.RequiredNamePrefix) + ")";
            if (p.Department != null)
            {
                filter += " and department eq " + DirectoryService.Literal(p.Department);
            }

            if (!p.IncludeDisabled)
            {
                filter += " and accountEnabled eq true";
            }

            // Graph has no case-insensitive "contains" filter for users, so name and job title are matched here.
            bool scanTruncated;
            var matches = ctx.Graph.GetAll("/users?" + DirectoryService.Filter(filter) + "&$count=true&$select=" + DirectoryService.UserSelect + "&$top=999",
                    MaxScanned, out scanTruncated, new GraphCallOptions { EventualConsistency = true })
                .Select(UserInfo.From)
                .Where(u => ctx.Validator.InScope(u.DisplayName))
                .Where(u => p.Name == null || Contains(u.DisplayName, p.Name))
                .Where(u => p.JobTitle == null || Contains(u.JobTitle, p.JobTitle))
                .OrderBy(u => u.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var shown = matches.Take(p.Top).ToList();
            var message = matches.Count == 0
                ? "No managed user matches the search."
                : "Found " + matches.Count + " user(s): " + string.Join(", ", shown.Select(u => u.DisplayName
                    + (string.IsNullOrEmpty(u.JobTitle) ? string.Empty : " (" + u.JobTitle + ")")))
                    + (matches.Count > shown.Count ? ", and more; narrow the search." : ".");

            return ActionResult.Ok(message, new JsonObject
            {
                { "users", new JsonArray(shown.Select(u => u.ToJson())) },
                { "total", matches.Count },
                { "truncated", scanTruncated || matches.Count > shown.Count }
            });
        }

        private static bool Contains(string value, string part)
        {
            return value != null && value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
