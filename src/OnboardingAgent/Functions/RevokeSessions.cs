using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Invalidates the user's refresh tokens and session cookies. Needs an ApprovalId (enforced by the router as well).
    /// </summary>
    public static class RevokeSessions
    {
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

        public static void Revoke(ActionContext ctx, UserInfo user)
        {
            ctx.Graph.Post("/users/" + user.Id + "/revokeSignInSessions", new JsonObject(), new GraphCallOptions { Idempotent = true });
        }
    }
}
