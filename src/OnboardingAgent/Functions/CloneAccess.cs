using System;
using System.Collections.Generic;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Copies the source user's direct group memberships to the target, skipping dynamic, role-assignable, protected
    /// and otherwise unmanageable groups. Returns a preview unless previewOnly is false.
    /// </summary>
    public static class CloneAccess
    {
        /// <summary>Plans which of the source user's groups to copy to the target user, and adds them unless previewOnly is true.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: sourceUserId, targetUserId and previewOnly.</param>
        /// <returns>The ActionResult sent back to the agent, with the plan and, when applied, one step per group added.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = CloneAccessParameters.Parse(parameters);
            var source = ctx.Validator.ReadableUser(p.SourceUserId);
            var target = ctx.Validator.WritableUser(p.TargetUserId);
            if (source.Id == target.Id)
            {
                throw new ValidationException("Source and target must be different users.");
            }

            var existing = new HashSet<string>(ctx.Directory.GetUserGroups(target.Id, false).Select(g => g.Id), StringComparer.OrdinalIgnoreCase);
            var report = new JsonArray();
            var toAdd = Plan(ctx, source, existing, report);

            var data = new JsonObject
            {
                { "previewOnly", p.PreviewOnly },
                { "source", source.ToJson() },
                { "target", target.ToJson() },
                { "plan", report }
            };

            if (p.PreviewOnly)
            {
                return ActionResult.Ok("Preview only, nothing was changed: " + toAdd.Count + " group(s) would be added to " + target.DisplayName
                    + ". Run again with previewOnly set to false to apply.", data);
            }

            var steps = ctx.NewStepLog();
            foreach (var group in toAdd)
            {
                steps.Run("Add to " + group.DisplayName, () =>
                    ctx.Directory.AddGroupMember(group.Id, target.Id, false) ? "added" : "already a member");
            }

            return ActionResult.FromSteps(steps,
                "Copied " + toAdd.Count + " group membership(s) from " + source.DisplayName + " to " + target.DisplayName + ".",
                "Not every group membership was copied.",
                data);
        }

        /// <summary>
        /// Lists the source's direct groups with an add or skip decision in report, and returns the groups to add.
        /// Names of groups outside the managed scope are not disclosed.
        /// </summary>
        public static List<GroupInfo> Plan(ActionContext ctx, UserInfo source, HashSet<string> targetGroupIds, JsonArray report)
        {
            var toAdd = new List<GroupInfo>();
            foreach (var group in ctx.Directory.GetUserGroups(source.Id, false).OrderBy(g => g.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                var skipReason = group.GetWriteBlockReason(ctx.Config);
                if (skipReason == null && targetGroupIds.Contains(group.Id))
                {
                    skipReason = "already a member";
                }

                var inScope = ctx.Validator.InScope(group.DisplayName);
                report.Add(new JsonObject
                {
                    { "groupId", inScope ? group.Id : null },
                    { "displayName", inScope ? group.DisplayName : "(outside managed scope)" },
                    { "action", skipReason == null ? "add" : "skip" },
                    { "reason", skipReason }
                });

                if (skipReason == null)
                {
                    toAdd.Add(group);
                }
            }

            return toAdd;
        }
    }
}
