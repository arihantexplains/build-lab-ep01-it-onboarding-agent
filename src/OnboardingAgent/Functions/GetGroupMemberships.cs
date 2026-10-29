using System;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// The user's groups. Groups outside the managed scope are counted but not named.
    /// </summary>
    public static class GetGroupMemberships
    {
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = GetGroupMembershipsParameters.Parse(parameters);
            var user = ctx.Validator.ReadableUser(p.UserId);

            var all = ctx.Directory.GetUserGroups(user.Id, p.Transitive);
            var inScope = all
                .Where(g => ctx.Validator.InScope(g.DisplayName))
                .OrderBy(g => g.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var outOfScope = all.Count - inScope.Count;

            return ActionResult.Ok(user.DisplayName + " is in " + inScope.Count + " managed group(s)"
                + (outOfScope > 0 ? " and " + outOfScope + " other group(s)." : "."), new JsonObject
            {
                { "user", user.ToJson() },
                { "transitive", p.Transitive },
                { "groups", new JsonArray(inScope.Select(g => g.ToJson(ctx.Config))) },
                { "outOfScopeGroupCount", outOfScope }
            });
        }
    }
}
