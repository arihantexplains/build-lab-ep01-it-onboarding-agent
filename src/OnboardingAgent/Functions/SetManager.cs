using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>Sets a managed user's manager. The manager only needs to be readable, and a user cannot be their own manager.</summary>
    public static class SetManager
    {
        /// <summary>Sets the user's manager, replacing any existing one.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId and managerId.</param>
        /// <returns>The ActionResult sent back to the agent, with the user and the new manager.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = SetManagerParameters.Parse(parameters);
            var user = ctx.Validator.WritableUser(p.UserId);
            var manager = ctx.Validator.ReadableUser(p.ManagerId);
            if (user.Id == manager.Id)
            {
                throw new ValidationException("A user cannot be their own manager.");
            }

            ctx.Directory.SetManager(user.Id, manager.Id, false);

            return ActionResult.Ok("Set " + manager.DisplayName + " as manager of " + user.DisplayName + ".", new JsonObject
            {
                { "user", user.ToJson() },
                { "manager", manager.ToJson() }
            });
        }
    }
}
