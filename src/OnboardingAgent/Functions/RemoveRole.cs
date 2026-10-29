using System;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Removes an allow-listed directory role from a user. Needs an ApprovalId (enforced by the router as well).
    /// Roles outside the allow-list are never touched, even to remove them. The user must still be in the managed
    /// administrative unit; the "no administrator" rule does not apply here, because the user holds the role being removed.
    /// </summary>
    public static class RemoveRole
    {
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            Validator.RequireApproval(ctx.ApprovalId, "RemoveRole");
            var p = RemoveRoleParameters.Parse(parameters);

            var role = AssignRole.ResolveRole(ctx, p.Role);
            var roleName = role.GetString("displayName");
            if (!ctx.Config.IsAssignableRole(roleName, role.GetString("templateId"), role.GetString("id")))
            {
                throw new ValidationException("Role '" + roleName + "' is not in the assignable roles allow-list, so this tool will not change it.");
            }

            var user = ctx.Validator.ReadableUser(p.UserId);
            ctx.Validator.RequireInAdminUnit(user);

            var matching = GetUserRoles.Assignments(ctx, user.Id)
                .Where(a => string.Equals(a.GetString("roleDefinitionId"), role.GetString("id"), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a.GetString("roleDefinitionId"), role.GetString("templateId"), StringComparison.OrdinalIgnoreCase))
                .ToList();
            var data = new JsonObject
            {
                { "user", user.ToJson() },
                { "role", roleName },
                { "approvalId", ctx.ApprovalId }
            };

            if (matching.Count == 0)
            {
                return ActionResult.Ok(user.DisplayName + " does not hold " + roleName + ", so nothing was changed.", data);
            }

            var steps = ctx.NewStepLog();
            foreach (var assignment in matching)
            {
                steps.Run("Remove " + roleName, () =>
                {
                    try
                    {
                        ctx.Graph.Delete("/roleManagement/directory/roleAssignments/" + DirectoryService.Segment(assignment.GetString("id")));
                        return "removed";
                    }
                    catch (GraphException ex) when (ex.StatusCode == 404)
                    {
                        return "already removed";
                    }
                });
            }

            return ActionResult.FromSteps(steps,
                "Removed " + roleName + " from " + user.DisplayName + ".",
                "Removing " + roleName + " from " + user.DisplayName + " did not fully succeed.",
                data);
        }
    }
}
