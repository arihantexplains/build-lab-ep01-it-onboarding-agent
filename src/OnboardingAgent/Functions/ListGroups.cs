using System;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    public static class ListGroups
    {
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = ListGroupsParameters.Parse(parameters);
            var prefix = ctx.Config.RequiredNamePrefix;
            var startsWith = p.NameStartsWith == null ? prefix
                : p.NameStartsWith.StartsWith(prefix, StringComparison.Ordinal) ? p.NameStartsWith
                : prefix + p.NameStartsWith;

            bool truncated;
            var groups = ctx.Graph.GetAll(
                    "/groups?" + DirectoryService.Filter("startswith(displayName," + DirectoryService.Literal(startsWith) + ")")
                    + "&$select=" + DirectoryService.GroupSelect + "&$top=" + p.Top,
                    p.Top, out truncated)
                .Select(GroupInfo.From)
                .Where(g => ctx.Validator.InScope(g.DisplayName))
                .OrderBy(g => g.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return ActionResult.Ok("Found " + groups.Count + " group(s)" + (truncated ? "; more exist, so narrow the search." : "."), new JsonObject
            {
                { "groups", new JsonArray(groups.Select(g => g.ToJson(ctx.Config))) },
                { "truncated", truncated }
            });
        }
    }
}
