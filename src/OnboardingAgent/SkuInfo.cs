namespace OnboardingAgent
{
    /// <summary>A licence product (SKU) the tenant subscribes to, with its licence counts.</summary>
    public sealed class SkuInfo
    {
        /// <summary>The SKU's ID.</summary>
        public string SkuId { get; private set; }

        /// <summary>The SKU's part number, for example ENTERPRISEPACK.</summary>
        public string SkuPartNumber { get; private set; }

        /// <summary>The number of active prepaid licences.</summary>
        public int Enabled { get; private set; }

        /// <summary>The number of licences already assigned.</summary>
        public int Consumed { get; private set; }

        /// <summary>The number of licences still free to assign.</summary>
        public int Available => Enabled - Consumed;

        /// <summary>Creates a SkuInfo from a subscribedSku object returned by Graph.</summary>
        /// <param name="json">The Graph subscribedSku.</param>
        public static SkuInfo From(JsonObject json)
        {
            return new SkuInfo
            {
                SkuId = json.GetString("skuId"),
                SkuPartNumber = json.GetString("skuPartNumber"),
                Enabled = json.GetObject("prepaidUnits")?.GetInt("enabled", 0) ?? 0,
                Consumed = json.GetInt("consumedUnits", 0)
            };
        }

        /// <summary>Converts the SKU to JSON with its ID, part number and number of free licences.</summary>
        public JsonObject ToJson()
        {
            return new JsonObject
            {
                { "skuId", SkuId },
                { "skuPartNumber", SkuPartNumber },
                { "available", Available }
            };
        }
    }
}
