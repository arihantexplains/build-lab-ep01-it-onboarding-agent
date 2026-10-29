using System;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// The users in a managed group. Members outside the managed scope (and non-user members) are counted but not named.
    /// </summary>
    public static class ListGroupMembers
    {
        /// <summary>Reads up to top members of the group and names the in-scope users among them.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: group (name or ID) and top.</param>
        /// <returns>The ActionResult sent back to the agent, with the named members, a count of the others and whether more exist.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = ListGroupMembersParameters.Parse(parameters);
            var group = ctx.Validator.ReadableGroup(p.Group);

            bool truncated;
            var members = ctx.Graph.GetAll("/groups/" + group.Id + "/members?$select=" + DirectoryService.UserSelect + "&$top=" + p.Top,
                p.Top, out truncated);
            var inScope = members
                .Where(m => string.Equals(m.GetString("@odata.type"), "#microsoft.graph.user", StringComparison.OrdinalIgnoreCase))
                .Select(UserInfo.From)
                .Where(u => ctx.Validator.InScope(u.DisplayName))
                .OrderBy(u => u.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var other = members.Count - inScope.Count;

            return ActionResult.Ok(group.DisplayName + " has " + inScope.Count + " managed member(s)"
                + (inScope.Count > 0 ? ": " + string.Join(", ", inScope.Select(u => u.DisplayName)) : string.Empty)
                + (other > 0 ? "; " + other + " other member(s) are not named" : string.Empty)
                + (truncated ? "; more exist, so raise 'top'." : "."), new JsonObject
            {
                { "group", group.ToJson(ctx.Config) },
                { "members", new JsonArray(inScope.Select(u => u.ToJson())) },
                { "otherMemberCount", other },
                { "truncated", truncated }
            });
        }
    }
}
