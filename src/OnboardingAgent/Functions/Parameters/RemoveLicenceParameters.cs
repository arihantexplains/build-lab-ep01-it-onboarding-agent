using System.Collections.Generic;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "skus": [ SKU part numbers or SKU IDs ] (also "licences"/"licenses") }</summary>
    public sealed class RemoveLicenceParameters
    {
        /// <summary>The user who loses the licences, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>SKU part numbers or SKU IDs from "skus" (also "licences", "licenses" or "sku"). Required, at least one. Blank and duplicate entries are dropped.</summary>
        public List<string> Skus { get; private set; }

        /// <summary>Reads and checks the parameters for RemoveLicence.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" is missing, or no SKU is listed.</exception>
        public static RemoveLicenceParameters Parse(JsonObject p)
        {
            var result = new RemoveLicenceParameters
            {
                UserId = p.RequireString("userId"),
                Skus = p.GetStringList(p.ResolveKey("skus", "licences", "licenses", "sku"))
            };

            if (result.Skus.Count == 0)
            {
                throw new ValidationException("Parameter 'skus' must list at least one licence.");
            }

            return result;
        }
    }
}
