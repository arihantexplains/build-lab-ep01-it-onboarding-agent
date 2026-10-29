using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>Changes allowed profile fields of a managed user. A new display name must keep the required prefix.</summary>
    public static class UpdateUser
    {
        /// <summary>Applies the requested field changes to the user in one Graph call.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId plus the fields to change. A field set to null or "" is cleared.</param>
        /// <returns>The ActionResult sent back to the agent, listing the fields changed.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = UpdateUserParameters.Parse(parameters);
            var user = ctx.Validator.WritableUser(p.UserId);

            object newName;
            if (p.Changes.TryGetValue("displayName", out newName))
            {
                // Renaming outside the prefix would move the user out of scope.
                ctx.Validator.RequirePrefix((string)newName, "New display name", (string)newName);
            }

            ctx.Graph.Patch("/users/" + user.Id, p.Changes);

            return ActionResult.Ok("Updated " + string.Join(", ", p.Changes.Keys) + " for " + user.DisplayName + ".", new JsonObject
            {
                { "user", user.ToJson() },
                { "changed", p.Changes }
            });
        }
    }
}
