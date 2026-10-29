using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    public static class GetSignInStatus
    {
        private const string BaseSelect = "id,displayName,userPrincipalName,accountEnabled,createdDateTime,lastPasswordChangeDateTime";

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
