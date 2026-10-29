namespace OnboardingAgent
{
    public sealed class SkuInfo
    {
        public string SkuId { get; private set; }

        public string SkuPartNumber { get; private set; }

        public int Enabled { get; private set; }

        public int Consumed { get; private set; }

        public int Available => Enabled - Consumed;

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
