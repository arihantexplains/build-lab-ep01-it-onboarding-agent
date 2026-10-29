using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Sets a generated temporary password and emails it to the user's manager. The password is never returned,
    /// logged or traced. Needs an ApprovalId (enforced by the router as well).
    /// </summary>
    public static class ResetPassword
    {
        /// <summary>Checks the user has a manager with a mailbox, then sets a new temporary password and emails it to that manager.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId and forceChangePasswordNextSignIn.</param>
        /// <returns>The ActionResult sent back to the agent, or a failure when the password was reset but the email could not be sent.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            Validator.RequireApproval(ctx.ApprovalId, "ResetPassword");
            var p = ResetPasswordParameters.Parse(parameters);
            var user = ctx.Validator.WritableUser(p.UserId);

            // Check the delivery route before changing anything, so a reset never leaves the user with an unknown password.
            var manager = ctx.Directory.GetManager(user.Id);
            if (manager == null)
            {
                throw new ValidationException(user.DisplayName + " has no manager to receive the temporary password. Set a manager first.");
            }

            ctx.Validator.RequirePrefix(manager.DisplayName, "Manager", manager.UserPrincipalName);
            ctx.Validator.RequireMailbox(manager);

            var password = TempPassword.Generate();
            ctx.Graph.Patch("/users/" + user.Id, new JsonObject
            {
                {
                    "passwordProfile", new JsonObject
                    {
                        { "forceChangePasswordNextSignIn", p.ForceChangeAtNextSignIn },
                        { "password", password }
                    }
                }
            });

            var data = new JsonObject
            {
                { "user", user.ToJson() },
                { "deliveredTo", manager.UserPrincipalName },
                { "forceChangePasswordNextSignIn", p.ForceChangeAtNextSignIn },
                { "approvalId", ctx.ApprovalId }
            };

            try
            {
                ctx.Directory.SendTemporaryPassword(manager, user, password,
                    "The password for your team member has been reset.", p.ForceChangeAtNextSignIn, ctx.CorrelationId);
            }
            catch (AdminActionException ex)
            {
                return ActionResult.Fail("The password for " + user.DisplayName + " was reset, but emailing it to " + manager.DisplayName
                    + " failed: " + ex.Message + " Run ResetPassword again to issue a new password.", data);
            }

            return ActionResult.Ok("Password reset for " + user.DisplayName + ". The temporary password was emailed to their manager, "
                + manager.DisplayName + ".", data);
        }
    }
}
