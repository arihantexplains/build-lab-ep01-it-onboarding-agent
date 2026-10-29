namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "maxDepth": 1-15, default 10 }</summary>
    public sealed class GetManagerChainParameters
    {
        /// <summary>The user to start from, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>How many manager levels to walk up at most, from "maxDepth". Optional, 1 to 15, default 10.</summary>
        public int MaxDepth { get; private set; }

        /// <summary>Reads and checks the parameters for GetManagerChain.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" is missing, or "maxDepth" is not a whole number from 1 to 15.</exception>
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
