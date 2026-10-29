namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>Parameters for actions that take only a user: { "userId": "id or UPN" }.</summary>
    public sealed class UserTargetParameters
    {
        public string UserId { get; private set; }

        public static UserTargetParameters Parse(JsonObject p)
        {
            return new UserTargetParameters { UserId = p.RequireString("userId") };
        }
    }
}
