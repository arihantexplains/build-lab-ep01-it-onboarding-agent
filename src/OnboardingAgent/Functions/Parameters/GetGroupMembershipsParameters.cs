namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "transitive": false by default, true includes nested groups }</summary>
    public sealed class GetGroupMembershipsParameters
    {
        public string UserId { get; private set; }

        public bool Transitive { get; private set; }

        public static GetGroupMembershipsParameters Parse(JsonObject p)
        {
            return new GetGroupMembershipsParameters
            {
                UserId = p.RequireString("userId"),
                Transitive = p.GetBool("transitive", false)
            };
        }
    }
}
