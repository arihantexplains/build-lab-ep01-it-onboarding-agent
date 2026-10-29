namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "transitive": false by default, true includes nested groups }</summary>
    public sealed class GetGroupMembershipsParameters
    {
        /// <summary>The user whose groups are listed, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>Whether nested group memberships are included, from "transitive". Optional, default false.</summary>
        public bool Transitive { get; private set; }

        /// <summary>Reads and checks the parameters for GetGroupMemberships.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" is missing, or "transitive" is not true or false.</exception>
        public static GetGroupMembershipsParameters Parse(JsonObject p)
        {
            return new GetGroupMembershipsParameters
            {
                UserId = p.RequireString("userId"),
                Transitive = p.GetBool("transitive", false)
            };
        }
    }
}
