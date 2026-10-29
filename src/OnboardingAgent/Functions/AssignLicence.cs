using System;
using System.Collections.Generic;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>Assigns one or more subscribed licences to a managed user, setting the usage location first when the user has none.</summary>
    public static class AssignLicence
    {
        /// <summary>Assigns the requested licences the user does not already have, after checking that free licences remain.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId, skus (part numbers or SKU IDs) and an optional usageLocation.</param>
        /// <returns>The ActionResult sent back to the agent, listing the licences assigned and those the user already had.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = AssignLicenceParameters.Parse(parameters);
            var user = ctx.Validator.WritableUser(p.UserId);
            var requested = ResolveSkus(ctx, p.Skus);

            var assigned = new HashSet<string>(
                ctx.Graph.Get("/users/" + user.Id + "?$select=assignedLicenses").GetArray("assignedLicenses").Objects()
                    .Select(licence => licence.GetString("skuId"))
                    .Where(id => id != null),
                StringComparer.OrdinalIgnoreCase);
            var toAssign = requested.Where(sku => !assigned.Contains(sku.SkuId)).ToList();

            var data = new JsonObject
            {
                { "user", user.ToJson() },
                { "alreadyAssigned", new JsonArray(requested.Where(sku => assigned.Contains(sku.SkuId)).Select(sku => sku.SkuPartNumber)) }
            };

            if (toAssign.Count == 0)
            {
                return ActionResult.Ok(user.DisplayName + " already has the requested licence(s).", data);
            }

            RequireAvailable(toAssign);

            if (string.IsNullOrEmpty(user.UsageLocation))
            {
                if (p.UsageLocation == null)
                {
                    throw new ValidationException(user.DisplayName + " has no usage location. Pass usageLocation (for example GB) so a licence can be assigned.");
                }

                ctx.Graph.Patch("/users/" + user.Id, new JsonObject { { "usageLocation", p.UsageLocation } });
                data["usageLocationSet"] = p.UsageLocation;
            }

            var detail = Assign(ctx, user.Id, toAssign, false);
            data["assigned"] = new JsonArray(toAssign.Select(sku => sku.SkuPartNumber));
            return ActionResult.Ok(detail + " to " + user.DisplayName + ".", data);
        }

        /// <summary>Matches SKU part numbers or IDs against the tenant's subscriptions.</summary>
        public static List<SkuInfo> ResolveSkus(ActionContext ctx, IEnumerable<string> requested)
        {
            var catalog = ctx.Graph.Get("/subscribedSkus").GetArray("value").Objects().Select(SkuInfo.From).ToList();
            var result = new List<SkuInfo>();
            foreach (var name in requested)
            {
                var sku = catalog.FirstOrDefault(s =>
                    string.Equals(s.SkuPartNumber, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s.SkuId, name, StringComparison.OrdinalIgnoreCase));
                if (sku == null)
                {
                    throw new ValidationException("Licence '" + name + "' is not subscribed in this tenant.");
                }

                if (!result.Contains(sku))
                {
                    result.Add(sku);
                }
            }

            return result;
        }

        /// <summary>Throws a ValidationException when any of the SKUs has no free licences left.</summary>
        /// <param name="skus">The SKUs about to be assigned.</param>
        public static void RequireAvailable(IEnumerable<SkuInfo> skus)
        {
            foreach (var sku in skus.Where(s => s.Available <= 0))
            {
                throw new ValidationException("No free " + sku.SkuPartNumber + " licences (" + sku.Consumed + " of " + sku.Enabled + " in use).");
            }
        }

        /// <summary>Assigns the SKUs to the user in one Graph call, with all service plans turned on.</summary>
        /// <param name="ctx">The request context.</param>
        /// <param name="userId">The object ID of the user.</param>
        /// <param name="skus">The SKUs to assign.</param>
        /// <param name="retryOnReplicationDelay">True for a user created seconds ago, so the call retries while Entra ID replicates it.</param>
        /// <returns>A short description such as "Assigned ENTERPRISEPACK" for the result message.</returns>
        public static string Assign(ActionContext ctx, string userId, IList<SkuInfo> skus, bool retryOnReplicationDelay)
        {
            var add = new JsonArray(skus.Select(sku => new JsonObject
            {
                { "skuId", sku.SkuId },
                { "disabledPlans", new JsonArray() }
            }));

            ctx.Graph.Post("/users/" + userId + "/assignLicense",
                new JsonObject { { "addLicenses", add }, { "removeLicenses", new JsonArray() } },
                new GraphCallOptions { Idempotent = true, RetryOnReplicationDelay = retryOnReplicationDelay });

            return "Assigned " + string.Join(", ", skus.Select(sku => sku.SkuPartNumber));
        }
    }
}
