using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>Reports whether a user can sign in, when they last signed in and when their password last changed.</summary>
    public static class GetSignInStatus
    {
        private const string BaseSelect = "id,displayName,userPrincipalName,accountEnabled,createdDateTime,lastPasswordChangeDateTime";

        /// <summary>Reads the sign-in status. When signInActivity is not available to the app, only the basic properties are returned.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId.</param>
        /// <returns>The ActionResult sent back to the agent, with the sign-in dates and whether sign-in activity was available.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = UserTargetParameters.Parse(parameters);
            var user = ctx.Validator.ReadableUser(p.UserId);

            JsonObject details;
            var activityAvailable = true;
            try
            {
                details = ctx.Graph.Get("/users/" + user.Id + "?$select=" + BaseSelect + ",signInActivity");
            }
            catch (GraphException ex) when (ex.StatusCode == 403 || ex.StatusCode == 400)
            {
                // signInActivity needs AuditLog.Read.All and Entra ID P1; report what is available without it.
                ctx.Trace.Trace("signInActivity unavailable: {0}.", ex.Code);
                activityAvailable = false;
                details = ctx.Graph.Get("/users/" + user.Id + "?$select=" + BaseSelect);
            }

            var activity = details.GetObject("signInActivity");
            var lastSignIn = activity?.GetString("lastSuccessfulSignInDateTime") ?? activity?.GetString("lastSignInDateTime");
            var enabled = details.GetBool("accountEnabled", false);

            var data = new JsonObject
            {
                { "user", user.ToJson() },
                { "accountEnabled", enabled },
                { "createdDateTime", details.GetString("createdDateTime") },
                { "lastPasswordChangeDateTime", details.GetString("lastPasswordChangeDateTime") },
                { "signInActivityAvailable", activityAvailable },
                { "lastSignInDateTime", activity?.GetString("lastSignInDateTime") },
                { "lastNonInteractiveSignInDateTime", activity?.GetString("lastNonInteractiveSignInDateTime") },
                { "lastSuccessfulSignInDateTime", activity?.GetString("lastSuccessfulSignInDateTime") }
            };

            var message = user.DisplayName + " is " + (enabled ? "enabled" : "disabled") + "; "
                + (!activityAvailable ? "sign-in activity is not available to this app."
                    : lastSignIn == null ? "no sign-in recorded."
                    : "last sign-in " + lastSignIn + ".");
            return ActionResult.Ok(message, data);
        }
    }
}
