using System;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Grants an allow-listed directory role, scoped to the administrative unit rather than the whole tenant.
    /// Needs an ApprovalId (enforced by the router as well).
    /// </summary>
    public static class AssignRole
    {
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            Validator.RequireApproval(ctx.ApprovalId, "AssignRole");
            var p = AssignRoleParameters.Parse(parameters);

            if (ctx.Config.AssignableRoles.Count == 0)
            {
                throw new ValidationException("No roles are allowed: environment variable " + Config.AssignableRolesVariable + " is empty.");
            }

            var role = ResolveRole(ctx, p.Role);
            var roleName = role.GetString("displayName");
            if (!ctx.Config.IsAssignableRole(roleName, role.GetString("templateId"), role.GetString("id")))
            {
                throw new ValidationException("Role '" + roleName + "' is not in the assignable roles allow-list.");
            }

            var user = ctx.Validator.WritableUser(p.UserId);
            var scope = "/administrativeUnits/" + ctx.Config.AdminUnitId;
            var data = new JsonObject
            {
                { "user", user.ToJson() },
                { "role", roleName },
                { "scope", scope },
                { "approvalId", ctx.ApprovalId }
            };

            try
            {
                var assignment = ctx.Graph.Post("/roleManagement/directory/roleAssignments", new JsonObject
                {
                    { "@odata.type", "#microsoft.graph.unifiedRoleAssignment" },
                    { "roleDefinitionId", role.GetString("id") },
                    { "principalId", user.Id },
                    { "directoryScopeId", scope }
                });
                data["assignmentId"] = assignment.GetString("id");
            }
            catch (GraphException ex) when (ex.IsConflict)
            {
                return ActionResult.Ok(user.DisplayName + " already holds " + roleName + " for the administrative unit.", data);
            }

            return ActionResult.Ok("Assigned " + roleName + " to " + user.DisplayName + ", scoped to the administrative unit.", data);
        }

        public static JsonObject ResolveRole(ActionContext ctx, string role)
        {
            Guid id;
            if (Guid.TryParse(role, out id))
            {
                return ctx.Graph.Get("/roleManagement/directory/roleDefinitions/" + id.ToString("D") + "?$select=id,displayName,templateId",
                    new GraphCallOptions { NotFoundMessage = "Role '" + role + "' was not found." });
            }

            var match = ctx.Graph.Get("/roleManagement/directory/roleDefinitions?"
                    + DirectoryService.Filter("displayName eq " + DirectoryService.Literal(role)) + "&$select=id,displayName,templateId")
                .GetArray("value").Objects().FirstOrDefault();
            if (match == null)
            {
                throw new ValidationException("Role '" + role + "' was not found.");
            }

            return match;
        }
    }
}
