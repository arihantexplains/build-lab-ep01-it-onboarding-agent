using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Every group is checked before any is changed, so one ineligible group refuses the whole request.
    /// </summary>
    public static class RemoveFromGroups
    {
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
