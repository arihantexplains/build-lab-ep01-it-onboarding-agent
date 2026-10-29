namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "role": role display name, template ID or definition ID }</summary>
    public sealed class AssignRoleParameters
    {
        /// <summary>The user who gets the role, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>The role from "role": display name, template ID or definition ID. Required.</summary>
        public string Role { get; private set; }

        /// <summary>Reads and checks the parameters for AssignRole.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" or "role" is missing.</exception>
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
