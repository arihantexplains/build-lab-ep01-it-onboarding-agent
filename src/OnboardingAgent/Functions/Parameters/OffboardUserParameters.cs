namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// { "userId": ..., "removeGroups": true by default, "removeLicences": true by default, "revokeSessions": true by default }.
    /// OffboardUser always needs an ApprovalId.
    /// </summary>
    public sealed class OffboardUserParameters
    {
        public string UserId { get; private set; }

        public bool RemoveGroups { get; private set; }

        public bool RemoveLicences { get; private set; }

        public bool RevokeSessions { get; private set; }

        public static OffboardUserParameters Parse(JsonObject p)
        {
            return new OffboardUserParameters
            {
                UserId = p.RequireString("userId"),
                RemoveGroups = p.GetBool("removeGroups", true),
                RemoveLicences = p.GetBool(p.ResolveKey("removeLicences", "removeLicenses"), true),
                RevokeSessions = p.GetBool("revokeSessions", true)
            };
        }
    }
}
