using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Allows sign-in again for a disabled user (for example a returning employee). Groups and licences are not restored.
    /// </summary>
    public static class EnableUser
    {
        /// <summary>Allows the user to sign in again. Nothing changes when they can already sign in.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId.</param>
        /// <returns>The ActionResult sent back to the agent, saying whether sign-in was turned back on.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = UserTargetParameters.Parse(parameters);
            var user = ctx.Validator.WritableUser(p.UserId);
            var data = new JsonObject { { "user", user.ToJson() } };

            if (user.AccountEnabled)
            {
                return ActionResult.Ok(user.DisplayName + " can already sign in, so nothing was changed.", data);
            }

            ctx.Graph.Patch("/users/" + user.Id, new JsonObject { { "accountEnabled", true } });
            data["accountEnabled"] = true;
            return ActionResult.Ok(user.DisplayName + " can sign in again. Group memberships and licences were not changed.", data);
        }
    }
}
