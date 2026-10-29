namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "maxDepth": 1-15, default 10 }</summary>
    public sealed class GetManagerChainParameters
    {
        public string UserId { get; private set; }

        public int MaxDepth { get; private set; }

        public static GetManagerChainParameters Parse(JsonObject p)
        {
            return new GetManagerChainParameters
            {
                UserId = p.RequireString("userId"),
                MaxDepth = p.GetInt("maxDepth", 10, 1, 15)
            };
        }
    }
}
