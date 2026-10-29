using System;
using System.Collections.Generic;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Walks up the manager chain, stopping at the top, at a loop, or at the first manager outside the managed scope.
    /// </summary>
    public static class GetManagerChain
    {
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = GetManagerChainParameters.Parse(parameters);
            var user = ctx.Validator.ReadableUser(p.UserId);

            var chain = new JsonArray();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { user.Id };
            var stoppedBecause = "maximum depth reached";
            var current = user;
            for (var level = 1; level <= p.MaxDepth; level++)
            {
                var manager = ctx.Directory.GetManager(current.Id);
                if (manager == null)
                {
                    stoppedBecause = "top of the chain reached";
                    break;
                }

                if (!seen.Add(manager.Id))
                {
                    stoppedBecause = "the chain loops back on itself";
                    break;
                }

                if (!ctx.Validator.InScope(manager.DisplayName))
                {
                    stoppedBecause = "the next manager is outside the managed scope";
                    break;
                }

                chain.Add(new JsonObject
                {
                    { "level", level },
                    { "id", manager.Id },
                    { "displayName", manager.DisplayName },
                    { "userPrincipalName", manager.UserPrincipalName },
                    { "jobTitle", manager.JobTitle }
                });
                current = manager;
            }

            return ActionResult.Ok(user.DisplayName + " has " + chain.Count + " manager(s) in the chain; stopped because " + stoppedBecause + ".", new JsonObject
            {
                { "user", user.ToJson() },
                { "chain", chain },
                { "stoppedBecause", stoppedBecause }
            });
        }
    }
}
