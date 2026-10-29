namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "revokeSessions": false by default }. DisableUser always needs an ApprovalId.</summary>
    public sealed class DisableUserParameters
    {
        /// <summary>The user to disable, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>Whether to also sign the user out of current sessions, from "revokeSessions". Optional, default false.</summary>
        public bool RevokeSessions { get; private set; }

        /// <summary>Reads and checks the parameters for DisableUser.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" is missing, or "revokeSessions" is not true or false.</exception>
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
