namespace OnboardingAgent
{
    /// <summary>The user properties this tool reads from Graph.</summary>
    public sealed class UserInfo
    {
        /// <summary>The user's object ID.</summary>
        public string Id { get; private set; }

        /// <summary>The user's display name, which must start with the required prefix to be managed.</summary>
        public string DisplayName { get; private set; }

        /// <summary>The user's sign-in name (UPN).</summary>
        public string UserPrincipalName { get; private set; }

        /// <summary>The user's email address, or null when they have no mailbox.</summary>
        public string Mail { get; private set; }

        /// <summary>True when the user is allowed to sign in.</summary>
        public bool AccountEnabled { get; private set; }

        /// <summary>The two-letter country code Graph needs before a licence can be assigned.</summary>
        public string UsageLocation { get; private set; }

        /// <summary>The user's department.</summary>
        public string Department { get; private set; }

        /// <summary>The user's job title.</summary>
        public string JobTitle { get; private set; }

        /// <summary>Creates a UserInfo from a user object returned by Graph.</summary>
        /// <param name="json">The Graph user, read with the properties in DirectoryService.UserSelect.</param>
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

        /// <summary>Converts the user to the JSON shape sent back to the agent. The usage location is left out.</summary>
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
