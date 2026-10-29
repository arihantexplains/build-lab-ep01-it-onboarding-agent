using System;
using System.Collections.Generic;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// The user's licences with their product names, and whether each one is assigned directly or through a group.
    /// </summary>
    public static class GetUserLicences
    {
        /// <summary>Lists the user's licences with their part numbers and how each one was assigned.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId.</param>
        /// <returns>The ActionResult sent back to the agent, with each licence marked as direct or group.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = UserTargetParameters.Parse(parameters);
            var user = ctx.Validator.ReadableUser(p.UserId);

            var catalog = ctx.Graph.Get("/subscribedSkus").GetArray("value").Objects().Select(SkuInfo.From)
                .Where(s => s.SkuId != null)
                .GroupBy(s => s.SkuId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var licences = new JsonArray();
            foreach (var entry in Licences(ctx, user.Id))
            {
                SkuInfo sku;
                catalog.TryGetValue(entry.Key, out sku);
                licences.Add(new JsonObject
                {
                    { "skuId", entry.Key },
                    { "skuPartNumber", sku?.SkuPartNumber },
                    { "assignedBy", entry.Value ? "direct" : "group" }
                });
            }

            var names = licences.Objects().Select(l => l.GetString("skuPartNumber") ?? l.GetString("skuId")).ToList();
            return ActionResult.Ok(names.Count == 0
                    ? user.DisplayName + " has no licences."
                    : user.DisplayName + " has " + names.Count + " licence(s): " + string.Join(", ", names) + ".",
                new JsonObject { { "user", user.ToJson() }, { "licences", licences } });
        }

        /// <summary>SKU IDs assigned directly (not inherited from a group), the only ones that can be removed per user.</summary>
        public static List<string> DirectSkuIds(ActionContext ctx, string userId)
        {
            return Licences(ctx, userId).Where(entry => entry.Value).Select(entry => entry.Key).ToList();
        }

        /// <summary>
        /// Every licence on the user (assignedLicenses) with true when it is assigned directly. licenseAssignmentStates says
        /// which ones come from a group; it can lag a few seconds behind assignedLicenses, so a licence without a state counts as direct.
        /// </summary>
        private static Dictionary<string, bool> Licences(ActionContext ctx, string userId)
        {
            var json = ctx.Graph.Get("/users/" + userId + "?$select=assignedLicenses,licenseAssignmentStates");
            var states = json.GetArray("licenseAssignmentStates").Objects().ToList();
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var skuId in json.GetArray("assignedLicenses").Objects().Select(l => l.GetString("skuId")).Where(id => id != null))
            {
                var forSku = states.Where(st => string.Equals(st.GetString("skuId"), skuId, StringComparison.OrdinalIgnoreCase)).ToList();
                result[skuId] = forSku.Count == 0 || forSku.Any(st => st.GetString("assignedByGroup") == null);
            }

            return result;
        }
    }
}
