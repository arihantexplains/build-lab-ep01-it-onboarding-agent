namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "group": group ID or exact display name (also "groupId"), "top": 1-500, default 100 }</summary>
    public sealed class ListGroupMembersParameters
    {
        public string Group { get; private set; }

        public int Top { get; private set; }

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
