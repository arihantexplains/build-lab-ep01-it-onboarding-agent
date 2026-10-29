namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// { "userId": ..., "removeGroups": true by default, "removeLicences": true by default, "revokeSessions": true by default }.
    /// OffboardUser always needs an ApprovalId.
    /// </summary>
    public sealed class OffboardUserParameters
    {
        /// <summary>The user to offboard, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>Whether to remove the user from groups, from "removeGroups". Optional, default true.</summary>
        public bool RemoveGroups { get; private set; }

        /// <summary>Whether to remove the user's licences, from "removeLicences" (or "removeLicenses"). Optional, default true.</summary>
        public bool RemoveLicences { get; private set; }

        /// <summary>Whether to sign the user out of current sessions, from "revokeSessions". Optional, default true.</summary>
        public bool RevokeSessions { get; private set; }

        /// <summary>Reads and checks the parameters for OffboardUser.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" is missing, or a flag is not true or false.</exception>
        public static OffboardUserParameters Parse(JsonObject p)
        {
            return new OffboardUserParameters
            {
                UserId = p.RequireString("userId"),
                RemoveGroups = p.GetBool("removeGroups", true),
                RemoveLicences = p.GetBool(p.ResolveKey("removeLicences", "removeLicenses"), true),
                RevokeSessions = p.GetBool("revokeSessions", true)
            };
        }
    }
}
