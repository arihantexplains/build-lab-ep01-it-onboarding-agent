using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Blocks sign-in, optionally also revoking sessions. Always needs an ApprovalId (enforced by the router).
    /// </summary>
    public static class DisableUser
    {
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = DisableUserParameters.Parse(parameters);
            if (p.RevokeSessions)
            {
                Validator.RequireApproval(ctx.ApprovalId, "Revoking sessions");
            }

            var user = ctx.Validator.WritableUser(p.UserId);

            var steps = ctx.NewStepLog();
            if (user.AccountEnabled)
            {
                steps.Run("Block sign-in", () =>
                {
                    ctx.Graph.Patch("/users/" + user.Id, new JsonObject { { "accountEnabled", false } });
                    return "disabled";
                });
            }
            else
            {
                steps.Done("Block sign-in", "already disabled");
            }

            if (p.RevokeSessions)
            {
                steps.Run("Revoke sessions", () =>
                {
                    RevokeSessions.Revoke(ctx, user);
                    return "revoked";
                });
            }

            return ActionResult.FromSteps(steps,
                user.DisplayName + " is disabled" + (p.RevokeSessions ? " and their sessions were revoked." : "."),
                "Disabling " + user.DisplayName + " did not fully succeed.",
                new JsonObject { { "user", user.ToJson() }, { "approvalId", ctx.ApprovalId } });
        }
    }
}
