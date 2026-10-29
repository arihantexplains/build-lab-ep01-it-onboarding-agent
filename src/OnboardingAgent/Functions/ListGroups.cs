using System;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>Lists managed groups, optionally only those whose names start with given text. The required prefix is always applied.</summary>
    public static class ListGroups
    {
        /// <summary>Finds in-scope groups whose names start with the required prefix plus any extra text given.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: an optional nameStartsWith and top.</param>
        /// <returns>The ActionResult sent back to the agent, with each group's type and whether this tool may change its membership.</returns>
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
