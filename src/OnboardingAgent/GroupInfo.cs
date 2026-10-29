using System;
using System.Collections.Generic;
using System.Linq;

namespace OnboardingAgent
{
    public sealed class GroupInfo
    {
        public string Id { get; private set; }

        public string DisplayName { get; private set; }

        public string Mail { get; private set; }

        public IList<string> GroupTypes { get; private set; }

        public string MembershipRule { get; private set; }

        public bool IsAssignableToRole { get; private set; }

        public bool MailEnabled { get; private set; }

        public bool SecurityEnabled { get; private set; }

        public bool OnPremisesSyncEnabled { get; private set; }

        public bool IsDynamic => HasType("DynamicMembership") || !string.IsNullOrEmpty(MembershipRule);

        public bool IsUnified => HasType("Unified");

        public string Kind => IsUnified ? "Microsoft 365"
            : SecurityEnabled && MailEnabled ? "Mail-enabled security"
            : SecurityEnabled ? "Security"
            : "Distribution";

        public static GroupInfo From(JsonObject json)
        {
            return new GroupInfo
            {
                Id = json.GetString("id"),
                DisplayName = json.GetString("displayName"),
                Mail = json.GetString("mail"),
                GroupTypes = json.GetArray("groupTypes").OfType<string>().ToList(),
                MembershipRule = json.GetString("membershipRule"),
                IsAssignableToRole = json.GetBool("isAssignableToRole", false),
                MailEnabled = json.GetBool("mailEnabled", false),
                SecurityEnabled = json.GetBool("securityEnabled", false),
                OnPremisesSyncEnabled = json.GetBool("onPremisesSyncEnabled", false)
            };
        }

        /// <summary>Why this tool must not change the group's membership, or null when it may.</summary>
        public string GetWriteBlockReason(Config config)
        {
            if (!config.HasRequiredPrefix(DisplayName))
            {
                return "its name does not start with the required prefix";
            }

            if (config.IsProtectedGroup(Id, DisplayName))
            {
                return "it is a protected group";
            }

            if (IsDynamic)
            {
                return "it has dynamic membership";
            }

            if (IsAssignableToRole)
            {
                return "it is role-assignable";
            }

            if (OnPremisesSyncEnabled)
            {
                return "it is synchronised from on-premises Active Directory";
            }

            if (MailEnabled && !IsUnified)
            {
                return "it is a distribution or mail-enabled security group, which is managed in Exchange";
            }

            return null;
        }

        public JsonObject ToJson(Config config)
        {
            var blockedReason = GetWriteBlockReason(config);
            return new JsonObject
            {
                { "id", Id },
                { "displayName", DisplayName },
                { "mail", Mail },
                { "type", Kind },
                { "dynamic", IsDynamic },
                { "roleAssignable", IsAssignableToRole },
                { "syncedFromOnPremises", OnPremisesSyncEnabled },
                { "protected", config.IsProtectedGroup(Id, DisplayName) },
                { "changeable", blockedReason == null },
                { "blockedReason", blockedReason }
            };
        }

        private bool HasType(string type)
        {
            return GroupTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
        }
    }
}
