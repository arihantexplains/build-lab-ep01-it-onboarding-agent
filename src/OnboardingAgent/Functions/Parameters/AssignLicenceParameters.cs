using System.Collections.Generic;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// { "userId": ..., "skus": [ SKU part numbers such as ENTERPRISEPACK, or SKU IDs ] (also "licences"/"licenses"),
    ///   "usageLocation": needed only if the user has none }
    /// </summary>
    public sealed class AssignLicenceParameters
    {
        public string UserId { get; private set; }

        public List<string> Skus { get; private set; }

        public string UsageLocation { get; private set; }

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
