using System;
using System.Collections.Generic;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Removes directly assigned licences. Licences inherited from a group are reported, not removed (change the group instead).
    /// </summary>
    public static class RemoveLicence
    {
        /// <summary>Removes the requested licences that are assigned directly to the user.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId and skus (part numbers or SKU IDs).</param>
        /// <returns>The ActionResult sent back to the agent, listing the licences removed and those left because they are not assigned directly.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = RemoveLicenceParameters.Parse(parameters);
            var user = ctx.Validator.WritableUser(p.UserId);
            var requested = AssignLicence.ResolveSkus(ctx, p.Skus);

            var direct = new HashSet<string>(GetUserLicences.DirectSkuIds(ctx, user.Id), StringComparer.OrdinalIgnoreCase);
            var toRemove = requested.Where(s => direct.Contains(s.SkuId)).ToList();
            var notDirect = requested.Where(s => !direct.Contains(s.SkuId)).Select(s => s.SkuPartNumber).ToList();

            var data = new JsonObject
            {
                { "user", user.ToJson() },
                { "notAssignedDirectly", new JsonArray(notDirect) }
            };

            if (toRemove.Count == 0)
            {
                return ActionResult.Ok(user.DisplayName + " has none of these licences assigned directly, so nothing was changed.", data);
            }

            Remove(ctx, user.Id, toRemove.Select(s => s.SkuId).ToList());
            data["removed"] = new JsonArray(toRemove.Select(s => s.SkuPartNumber));
            return ActionResult.Ok("Removed " + string.Join(", ", toRemove.Select(s => s.SkuPartNumber)) + " from " + user.DisplayName + "."
                + (notDirect.Count > 0 ? " Not assigned directly, so left alone: " + string.Join(", ", notDirect) + "." : string.Empty), data);
        }

        /// <summary>Removes the given licences from the user in one Graph call.</summary>
        /// <param name="ctx">The request context.</param>
        /// <param name="userId">The object ID of the user.</param>
        /// <param name="skuIds">The SKU IDs to remove. They must be assigned directly.</param>
        public static void Remove(ActionContext ctx, string userId, IList<string> skuIds)
        {
            ctx.Graph.Post("/users/" + userId + "/assignLicense",
                new JsonObject { { "addLicenses", new JsonArray() }, { "removeLicenses", new JsonArray(skuIds.Cast<object>()) } },
                new GraphCallOptions { Idempotent = true });
        }
    }
}
