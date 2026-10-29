namespace OnboardingAgent
{
    public sealed class UserInfo
    {
        public string Id { get; private set; }

        public string DisplayName { get; private set; }

        public string UserPrincipalName { get; private set; }

        public string Mail { get; private set; }

        public bool AccountEnabled { get; private set; }

        public string UsageLocation { get; private set; }

        public string Department { get; private set; }

        public string JobTitle { get; private set; }

        public static UserInfo From(JsonObject json)
        {
            return new UserInfo
            {
                Id = json.GetString("id"),
                DisplayName = json.GetString("displayName"),
                UserPrincipalName = json.GetString("userPrincipalName"),
                Mail = json.GetString("mail"),
                AccountEnabled = json.GetBool("accountEnabled", false),
                UsageLocation = json.GetString("usageLocation"),
                Department = json.GetString("department"),
                JobTitle = json.GetString("jobTitle")
            };
        }

        public JsonObject ToJson()
        {
            return new JsonObject
            {
                { "id", Id },
                { "displayName", DisplayName },
                { "userPrincipalName", UserPrincipalName },
                { "mail", Mail },
                { "department", Department },
                { "jobTitle", JobTitle },
                { "accountEnabled", AccountEnabled }
            };
        }
    }
}
