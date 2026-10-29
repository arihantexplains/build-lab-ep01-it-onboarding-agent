using System.Collections.Generic;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "skus": [ SKU part numbers or SKU IDs ] (also "licences"/"licenses") }</summary>
    public sealed class RemoveLicenceParameters
    {
        public string UserId { get; private set; }

        public List<string> Skus { get; private set; }

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
