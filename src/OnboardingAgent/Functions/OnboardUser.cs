using System.Collections.Generic;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Create, set manager, assign licences, add groups (listed or cloned), then email the temporary password to the manager.
    /// Every input is validated first; with previewOnly (the default) nothing is changed.
    /// The welcome email is optional: with no mailbox (manager or sender) the user is still created and the step is reported as skipped.
    /// The temporary password is never returned, logged or shown.
    /// </summary>
    public static class OnboardUser
    {
        /// <summary>Validates every input, then returns a preview or creates and sets up the new user.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: the new user's profile fields, managerId, licences, groups or cloneFromUserId, sendWelcomeEmail and previewOnly.</param>
        /// <returns>The ActionResult sent back to the agent, with the preview or one step per action. The temporary password is never included.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = OnboardUserParameters.Parse(parameters, ctx.Config);
            var newUser = p.User;

            ctx.Validator.RequirePrefix(newUser.DisplayName, "User", newUser.DisplayName);
            if (ctx.Directory.UserExists(newUser.UserPrincipalName))
            {
                throw new ValidationException("A user with the UPN " + newUser.UserPrincipalName + " already exists.");
            }

            var manager = ctx.Validator.ReadableUser(p.ManagerId);
            string emailSkipped = null;
            if (p.SendWelcomeEmail && (string.IsNullOrEmpty(manager.Mail) || !ctx.Directory.HasMailbox(ctx.Config.SenderMailbox)))
            {
                emailSkipped = "Welcome email skipped: no mailbox in this tenant.";
            }

            var sendEmail = p.SendWelcomeEmail && emailSkipped == null;

            var skus = new List<SkuInfo>();
            if (p.Licences.Count > 0)
            {
                skus = AssignLicence.ResolveSkus(ctx, p.Licences);
                AssignLicence.RequireAvailable(skus);
            }

            var cloneReport = new JsonArray();
            List<GroupInfo> groups;
            if (p.CloneFromUserId != null)
            {
                var source = ctx.Validator.ReadableUser(p.CloneFromUserId);
                groups = CloneAccess.Plan(ctx, source, new HashSet<string>(), cloneReport);
            }
            else
            {
                groups = p.Groups.Select(ctx.Validator.WritableGroup).GroupBy(g => g.Id).Select(g => g.First()).ToList();
            }

            var data = new JsonObject
            {
                { "previewOnly", p.PreviewOnly },
                { "user", newUser.ToPreview() },
                { "manager", manager.ToJson() },
                { "licences", new JsonArray(skus.Select(s => s.ToJson())) },
                { "groups", new JsonArray(groups.Select(g => g.ToJson(ctx.Config))) },
                { "welcomeEmailTo", sendEmail ? manager.Mail : null },
                { "welcomeEmail", sendEmail ? "will be sent to the manager" : emailSkipped ?? "not requested" }
            };
            if (p.CloneFromUserId != null)
            {
                data["clonePlan"] = cloneReport;
            }

            if (p.PreviewOnly)
            {
                return ActionResult.Ok("Preview only, nothing was changed. Run again with previewOnly set to false to onboard "
                    + newUser.DisplayName + "." + (emailSkipped == null ? string.Empty : " " + emailSkipped), data);
            }

            var steps = ctx.NewStepLog();
            var password = TempPassword.Generate();
            UserInfo created = null;
            steps.Run("Create user", () =>
            {
                created = ctx.Directory.CreateUser(newUser.ToGraphUser(password));
                return created.UserPrincipalName + " (" + created.Id + ")";
            });

            if (created == null)
            {
                return ActionResult.FromSteps(steps, null, "The user was not created.", data);
            }

            data["createdUserId"] = created.Id;

            // Least privilege: the app has no AdministrativeUnit.ReadWrite.All, so an administrator adds new users to the unit.
            if (ctx.Directory.TryAddToAdminUnit(created.Id))
            {
                steps.Done("Add to administrative unit", "added");
            }
            else
            {
                steps.Skip("Add to administrative unit", "Not added: the app may not change the administrative unit, on purpose. An administrator adds the user.");
            }

            // The user is seconds old, so each step retries briefly while Entra ID replicates it.
            steps.Run("Set manager", () =>
            {
                ctx.Directory.SetManager(created.Id, manager.Id, true);
                return manager.DisplayName;
            });

            if (skus.Count > 0)
            {
                steps.Run("Assign licences", () => AssignLicence.Assign(ctx, created.Id, skus, true));
            }

            foreach (var group in groups)
            {
                steps.Run("Add to " + group.DisplayName, () =>
                    ctx.Directory.AddGroupMember(group.Id, created.Id, true) ? "added" : "already a member");
            }

            if (sendEmail)
            {
                steps.Run("Send welcome email", () =>
                {
                    try
                    {
                        ctx.Directory.SendTemporaryPassword(manager, created, password, "A new user has been created for your team member.", true, ctx.CorrelationId);
                    }
                    catch (AdminActionException ex)
                    {
                        throw new AdminActionException(ex.Message + " The temporary password was not delivered; use ResetPassword to issue a new one.");
                    }

                    return "sent to " + manager.Mail;
                });
            }
            else if (emailSkipped != null)
            {
                steps.Skip("Send welcome email", emailSkipped + " The temporary password was discarded. Use ResetPassword to issue a new one once a mailbox exists.");
            }
            else
            {
                steps.Skip("Send welcome email", "sendWelcomeEmail was false, so the temporary password was discarded. Use ResetPassword to issue one.");
            }

            return ActionResult.FromSteps(steps,
                "Onboarded " + created.DisplayName + "." + (emailSkipped == null ? string.Empty : " " + emailSkipped),
                created.DisplayName + " was created, but some steps failed.",
                data);
        }
    }
}
