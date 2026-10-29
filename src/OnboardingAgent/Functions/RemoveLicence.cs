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

        public static void Remove(ActionContext ctx, string userId, IList<string> skuIds)
        {
            ctx.Graph.Post("/users/" + userId + "/assignLicense",
                new JsonObject { { "addLicenses", new JsonArray() }, { "removeLicenses", new JsonArray(skuIds.Cast<object>()) } },
                new GraphCallOptions { Idempotent = true });
        }
    }
}
