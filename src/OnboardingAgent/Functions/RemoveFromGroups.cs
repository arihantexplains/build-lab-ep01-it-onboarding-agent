using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Removes a managed user from one or more managed groups. Every group is checked before any is changed, so one ineligible group refuses the whole request.
    /// </summary>
    public static class RemoveFromGroups
    {
        /// <summary>Removes the user from each requested group, noting groups they were not in.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId and groups (group names or IDs).</param>
        /// <returns>The ActionResult sent back to the agent, with one step per group.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = GroupChangeParameters.Parse(parameters);
            var user = ctx.Validator.WritableUser(p.UserId);
            var groups = p.Groups.Select(ctx.Validator.WritableGroup).GroupBy(g => g.Id).Select(g => g.First()).ToList();

            var steps = ctx.NewStepLog();
            foreach (var group in groups)
            {
                steps.Run("Remove from " + group.DisplayName, () =>
                    ctx.Directory.RemoveGroupMember(group.Id, user.Id) ? "removed" : "was not a member");
            }

            return ActionResult.FromSteps(steps,
                user.DisplayName + " is no longer a member of " + groups.Count + " group(s).",
                "Not every group was updated for " + user.DisplayName + ".",
                new JsonObject { { "user", user.ToJson() } });
        }
    }
}
