using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Invalidates the user's refresh tokens and session cookies. Needs an ApprovalId (enforced by the router as well).
    /// </summary>
    public static class RevokeSessions
    {
        /// <summary>Checks the approval, then revokes all sign-in sessions for the user.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId.</param>
        /// <returns>The ActionResult sent back to the agent, with the approval ID.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            Validator.RequireApproval(ctx.ApprovalId, "RevokeSessions");
            var p = UserTargetParameters.Parse(parameters);
            var user = ctx.Validator.WritableUser(p.UserId);

            Revoke(ctx, user);

            return ActionResult.Ok("Revoked all sign-in sessions for " + user.DisplayName + ". Existing access tokens can stay valid for up to an hour.", new JsonObject
            {
                { "user", user.ToJson() },
                { "approvalId", ctx.ApprovalId }
            });
        }

        /// <summary>Calls Graph revokeSignInSessions for the user. DisableUser and OffboardUser use it too.</summary>
        /// <param name="ctx">The request context.</param>
        /// <param name="user">The user whose sessions are revoked.</param>
        public static void Revoke(ActionContext ctx, UserInfo user)
        {
            ctx.Graph.Post("/users/" + user.Id + "/revokeSignInSessions", new JsonObject(), new GraphCallOptions { Idempotent = true });
        }
    }
}
