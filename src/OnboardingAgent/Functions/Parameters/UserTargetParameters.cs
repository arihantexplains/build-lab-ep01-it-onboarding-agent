namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>Parameters for actions that take only a user: { "userId": "id or UPN" }.</summary>
    public sealed class UserTargetParameters
    {
        /// <summary>The user, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>Reads and checks the parameters for an action that takes only a user.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" is missing.</exception>
        public static UserTargetParameters Parse(JsonObject p)
        {
            return new UserTargetParameters { UserId = p.RequireString("userId") };
        }
    }
}
