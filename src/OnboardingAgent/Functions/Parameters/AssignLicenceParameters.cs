using System.Collections.Generic;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// { "userId": ..., "skus": [ SKU part numbers such as ENTERPRISEPACK, or SKU IDs ] (also "licences"/"licenses"),
    ///   "usageLocation": needed only if the user has none }
    /// </summary>
    public sealed class AssignLicenceParameters
    {
        /// <summary>The user who gets the licences, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>SKU part numbers or SKU IDs from "skus" (also "licences", "licenses" or "sku"). Required, at least one. Blank and duplicate entries are dropped.</summary>
        public List<string> Skus { get; private set; }

        /// <summary>Two-letter country code from "usageLocation", in upper case. Optional, null when not given; only needed if the user has none yet.</summary>
        public string UsageLocation { get; private set; }

        /// <summary>Reads and checks the parameters for AssignLicence.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" is missing, no SKU is listed, or usageLocation is not a two-letter code.</exception>
        public static AssignLicenceParameters Parse(JsonObject p)
        {
            var result = new AssignLicenceParameters
            {
                UserId = p.RequireString("userId"),
                Skus = p.GetStringList(p.ResolveKey("skus", "licences", "licenses", "sku")),
                UsageLocation = NewUserParameters.ParseUsageLocation(p.GetTrimmed("usageLocation"))
            };

            if (result.Skus.Count == 0)
            {
                throw new ValidationException("Parameter 'skus' must list at least one licence.");
            }

            return result;
        }
    }
}
