using System;
using System.Collections.Generic;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// The directory roles a user holds through direct assignments, with the scope of each (tenant or administrative unit).
    /// </summary>
    public static class GetUserRoles
    {
        /// <summary>Lists the user's directory role assignments and the scope of each.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId.</param>
        /// <returns>The ActionResult sent back to the agent, with each role, its scope and its assignment ID.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = UserTargetParameters.Parse(parameters);
            var user = ctx.Validator.ReadableUser(p.UserId);

            var roles = Assignments(ctx, user.Id).Select(a => new JsonObject
            {
                { "role", a.GetString("roleName") },
                { "scope", Scope(ctx, a.GetString("directoryScopeId")) },
                { "assignmentId", a.GetString("id") }
            }).ToList();

            return ActionResult.Ok(roles.Count == 0
                    ? user.DisplayName + " holds no directory roles."
                    : user.DisplayName + " holds " + string.Join(", ", roles.Select(r => r.GetString("role") + " (" + r.GetString("scope") + ")")) + ".",
                new JsonObject { { "user", user.ToJson() }, { "roles", new JsonArray(roles) } });
        }

        /// <summary>The user's role assignments, each with roleName added from its role definition.</summary>
        public static List<JsonObject> Assignments(ActionContext ctx, string userId)
        {
            var assignments = ctx.Graph.Get("/roleManagement/directory/roleAssignments?"
                    + DirectoryService.Filter("principalId eq " + DirectoryService.Literal(userId))
                    + "&$select=id,roleDefinitionId,directoryScopeId")
                .GetArray("value").Objects().ToList();

            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assignment in assignments)
            {
                var definitionId = assignment.GetString("roleDefinitionId") ?? string.Empty;
                string name;
                if (!names.TryGetValue(definitionId, out name))
                {
                    name = ctx.Graph.Get("/roleManagement/directory/roleDefinitions/" + DirectoryService.Segment(definitionId) + "?$select=displayName")
                        .GetString("displayName");
                    names[definitionId] = name;
                }

                assignment["roleName"] = name;
            }

            return assignments;
        }

        private static string Scope(ActionContext ctx, string directoryScopeId)
        {
            if (string.IsNullOrEmpty(directoryScopeId) || directoryScopeId == "/")
            {
                return "whole tenant";
            }

            return directoryScopeId.Equals("/administrativeUnits/" + ctx.Config.AdminUnitId, StringComparison.OrdinalIgnoreCase)
                ? "managed administrative unit"
                : "other scope";
        }
    }
}
