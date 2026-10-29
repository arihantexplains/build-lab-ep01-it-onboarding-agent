namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "role": role display name, template ID or definition ID }</summary>
    public sealed class AssignRoleParameters
    {
        public string UserId { get; private set; }

        public string Role { get; private set; }

        public static AssignRoleParameters Parse(JsonObject p)
        {
            return new AssignRoleParameters
            {
                UserId = p.RequireString("userId"),
                Role = p.RequireString("role")
            };
        }
    }
}
