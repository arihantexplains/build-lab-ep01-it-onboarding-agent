namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "revokeSessions": false by default }. DisableUser always needs an ApprovalId.</summary>
    public sealed class DisableUserParameters
    {
        public string UserId { get; private set; }

        public bool RevokeSessions { get; private set; }

        public static DisableUserParameters Parse(JsonObject p)
        {
            return new DisableUserParameters
            {
                UserId = p.RequireString("userId"),
                RevokeSessions = p.GetBool("revokeSessions", false)
            };
        }
    }
}
