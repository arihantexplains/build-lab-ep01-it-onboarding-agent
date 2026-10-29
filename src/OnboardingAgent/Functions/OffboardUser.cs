using System;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Leaver in one call: block sign-in, revoke sessions, remove the user from every changeable group and remove directly
    /// assigned licences. Needs an ApprovalId (enforced by the router as well). Protected, dynamic and out-of-scope groups
    /// and group-based licences are left alone and reported as skipped. Nothing is deleted.
    /// </summary>
    public static class OffboardUser
    {
        /// <summary>Checks the approval, then blocks sign-in and runs the optional leaver steps.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId, revokeSessions, removeGroups and removeLicences.</param>
        /// <returns>The ActionResult sent back to the agent, with one step per action. The user object is kept.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            Validator.RequireApproval(ctx.ApprovalId, "OffboardUser");
            var p = OffboardUserParameters.Parse(parameters);
            var user = ctx.Validator.WritableUser(p.UserId);

            var steps = ctx.NewStepLog();
            if (user.AccountEnabled)
            {
                steps.Run("Block sign-in", () =>
                {
                    ctx.Graph.Patch("/users/" + user.Id, new JsonObject { { "accountEnabled", false } });
                    return "sign-in blocked";
                });
            }
            else
            {
                steps.Done("Block sign-in", "already blocked");
            }

            if (p.RevokeSessions)
            {
                steps.Run("Revoke sessions", () =>
                {
                    RevokeSessions.Revoke(ctx, user);
                    return "revoked";
                });
            }
            else
            {
                steps.Skip("Revoke sessions", "not requested");
            }

            if (p.RemoveGroups)
            {
                var outOfScope = 0;
                foreach (var group in ctx.Directory.GetUserGroups(user.Id, false).OrderBy(g => g.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    if (!ctx.Validator.InScope(group.DisplayName))
                    {
                        outOfScope++;
                        continue;
                    }

                    var blocked = group.GetWriteBlockReason(ctx.Config);
                    if (blocked != null)
                    {
                        steps.Skip("Remove from " + group.DisplayName, "kept: " + blocked);
                        continue;
                    }

                    steps.Run("Remove from " + group.DisplayName, () =>
                        ctx.Directory.RemoveGroupMember(group.Id, user.Id) ? "removed" : "was not a member");
                }

                if (outOfScope > 0)
                {
                    steps.Skip("Other groups", outOfScope + " group(s) outside the managed scope were left unchanged.");
                }
            }
            else
            {
                steps.Skip("Remove from groups", "not requested");
            }

            if (p.RemoveLicences)
            {
                var direct = GetUserLicences.DirectSkuIds(ctx, user.Id);
                if (direct.Count == 0)
                {
                    steps.Done("Remove licences", "no directly assigned licences");
                }
                else
                {
                    steps.Run("Remove licences", () =>
                    {
                        RemoveLicence.Remove(ctx, user.Id, direct);
                        return direct.Count + " licence(s) removed";
                    });
                }
            }
            else
            {
                steps.Skip("Remove licences", "not requested");
            }

            return ActionResult.FromSteps(steps,
                user.DisplayName + " is offboarded: sign-in blocked" + (p.RevokeSessions ? ", sessions revoked" : string.Empty)
                    + (p.RemoveGroups ? ", removed from managed groups" : string.Empty)
                    + (p.RemoveLicences ? ", licences removed" : string.Empty) + ". The user object is kept.",
                "Offboarding " + user.DisplayName + " did not fully succeed.",
                new JsonObject { { "user", user.ToJson() }, { "approvalId", ctx.ApprovalId } });
        }
    }
}
