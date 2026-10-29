namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "forceChangePasswordNextSignIn": true by default }</summary>
    public sealed class ResetPasswordParameters
    {
        /// <summary>The user whose password is reset, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>Whether the user must pick a new password at the next sign-in, from "forceChangePasswordNextSignIn". Optional, default true.</summary>
        public bool ForceChangeAtNextSignIn { get; private set; }

        /// <summary>Reads and checks the parameters for ResetPassword.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" is missing, or "forceChangePasswordNextSignIn" is not true or false.</exception>
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
