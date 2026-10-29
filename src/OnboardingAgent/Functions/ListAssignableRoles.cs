using System;
using System.Linq;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// The allow-list: the only directory roles AssignRole may grant (environment variable ab_AssignableRoles). No parameters.
    /// </summary>
    public static class ListAssignableRoles
    {
        /// <summary>Lists the directory roles that match the allow-list.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">Not used: this operation takes no parameters.</param>
        /// <returns>The ActionResult sent back to the agent, with the name, description and template ID of each role.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            if (ctx.Config.AssignableRoles.Count == 0)
            {
                return ActionResult.Ok("No roles can be assigned: the allow-list (" + Config.AssignableRolesVariable + ") is empty.",
                    new JsonObject { { "roles", new JsonArray() } });
            }

            bool truncated;
            var roles = ctx.Graph.GetAll("/roleManagement/directory/roleDefinitions?$select=id,displayName,description,templateId", 1000, out truncated)
                .Where(r => ctx.Config.IsAssignableRole(r.GetString("displayName"), r.GetString("templateId"), r.GetString("id")))
                .OrderBy(r => r.GetString("displayName"), StringComparer.OrdinalIgnoreCase)
                .ToList();

            return ActionResult.Ok(roles.Count + " role(s) can be assigned, each only with the IT lead's approval: "
                + string.Join(", ", roles.Select(r => r.GetString("displayName"))) + ".", new JsonObject
            {
                { "roles", new JsonArray(roles.Select(r => new JsonObject
                    {
                        { "displayName", r.GetString("displayName") },
                        { "description", r.GetString("description") },
                        { "templateId", r.GetString("templateId") }
                    })) },
                { "scope", "administrative unit" }
            });
        }
    }
}
