namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "group": group ID or exact display name (also "groupId"), "top": 1-500, default 100 }</summary>
    public sealed class ListGroupMembersParameters
    {
        /// <summary>Group ID or exact display name from "group" (also "groupId" or "groupName"). Required.</summary>
        public string Group { get; private set; }

        /// <summary>Maximum number of members to return, from "top". Optional, 1 to 500, default 100.</summary>
        public int Top { get; private set; }

        /// <summary>Reads and checks the parameters for ListGroupMembers.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">The group is missing, or "top" is not a whole number from 1 to 500.</exception>
        public static ListGroupMembersParameters Parse(JsonObject p)
        {
            return new ListGroupMembersParameters
            {
                Group = p.RequireString(p.ResolveKey("group", "groupId", "groupName")),
                Top = p.GetInt("top", 100, 1, 500)
            };
        }
    }
}
