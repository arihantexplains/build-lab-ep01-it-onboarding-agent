namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "managerId": ... }</summary>
    public sealed class SetManagerParameters
    {
        /// <summary>The user who gets the manager, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>The new manager, by object ID or UPN, from "managerId". Required.</summary>
        public string ManagerId { get; private set; }

        /// <summary>Reads and checks the parameters for SetManager.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" or "managerId" is missing.</exception>
        public static SetManagerParameters Parse(JsonObject p)
        {
            return new SetManagerParameters
            {
                UserId = p.RequireString("userId"),
                ManagerId = p.RequireString("managerId")
            };
        }
    }
}
