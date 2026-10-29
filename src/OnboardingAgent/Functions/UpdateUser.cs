using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    public static class UpdateUser
    {
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = UpdateUserParameters.Parse(parameters);
            var user = ctx.Validator.WritableUser(p.UserId);

            object newName;
            if (p.Changes.TryGetValue("displayName", out newName))
            {
                // Renaming outside the prefix would move the account out of scope.
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
