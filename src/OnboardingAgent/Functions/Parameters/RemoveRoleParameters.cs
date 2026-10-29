namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "role": role display name, template ID or definition ID }. RemoveRole always needs an ApprovalId.</summary>
    public sealed class RemoveRoleParameters
    {
        /// <summary>The user who loses the role, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>The role from "role" (or "roleName"): display name, template ID or definition ID. Required.</summary>
        public string Role { get; private set; }

        /// <summary>Reads and checks the parameters for RemoveRole.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" or "role" is missing.</exception>
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
