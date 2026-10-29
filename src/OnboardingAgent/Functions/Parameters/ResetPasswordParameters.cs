namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "forceChangePasswordNextSignIn": true by default }</summary>
    public sealed class ResetPasswordParameters
    {
        public string UserId { get; private set; }

        public bool ForceChangeAtNextSignIn { get; private set; }

        public static ResetPasswordParameters Parse(JsonObject p)
        {
            return new ResetPasswordParameters
            {
                UserId = p.RequireString("userId"),
                ForceChangeAtNextSignIn = p.GetBool("forceChangePasswordNextSignIn", true)
            };
        }
    }
}
