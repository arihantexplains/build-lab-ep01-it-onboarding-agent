namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "role": role display name, template ID or definition ID }. RemoveRole always needs an ApprovalId.</summary>
    public sealed class RemoveRoleParameters
    {
        public string UserId { get; private set; }

        public string Role { get; private set; }

        public static RemoveRoleParameters Parse(JsonObject p)
        {
            return new RemoveRoleParameters
            {
                UserId = p.RequireString("userId"),
                Role = p.RequireString(p.ResolveKey("role", "roleName"))
            };
        }
    }
}
