using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Creates an account inside the administrative unit. With managerId the manager is set and receives the temporary
    /// password; without it the password is discarded and ResetPassword issues one later.
    /// </summary>
    public static class CreateUser
    {
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = NewUserParameters.Parse(parameters, ctx.Config);

            ctx.Validator.RequirePrefix(p.DisplayName, "User", p.DisplayName);
            if (ctx.Directory.UserExists(p.UserPrincipalName))
            {
                throw new ValidationException("A user with the UPN " + p.UserPrincipalName + " already exists.");
            }

            UserInfo manager = null;
            if (p.ManagerId != null)
            {
                manager = ctx.Validator.ReadableUser(p.ManagerId);
                ctx.Validator.RequireMailbox(manager);
            }

            var password = TempPassword.Generate();
            var created = ctx.Directory.CreateUser(p.ToGraphUser(password));

            var steps = ctx.NewStepLog();
            steps.Done("Create user", created.UserPrincipalName + " (" + created.Id + ")");
            steps.Run("Add to administrative unit", () =>
            {
                ctx.Directory.AddToAdminUnit(created.Id);
                return "added";
            });
            var data = new JsonObject { { "user", created.ToJson() } };

            if (manager == null)
            {
                steps.Skip("Deliver temporary password", "No managerId was given, so the temporary password was discarded. Use ResetPassword to issue one.");
            }
            else
            {
                steps.Run("Set manager", () =>
                {
                    ctx.Directory.SetManager(created.Id, manager.Id, true);
                    return manager.DisplayName;
                });
                steps.Run("Deliver temporary password", () =>
                {
                    ctx.Directory.SendTemporaryPassword(manager, created, password, "A new user has been created for your team member.", true, ctx.CorrelationId);
                    return "sent to " + manager.Mail;
                });
            }

            return ActionResult.FromSteps(steps,
                "Created " + created.DisplayName + ".",
                created.DisplayName + " was created, but some steps failed.",
                data);
        }
    }
}
