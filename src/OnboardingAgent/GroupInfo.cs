using System;
using System.Collections.Generic;
using System.Linq;

namespace OnboardingAgent
{
    /// <summary>A directory group read from Graph, with the checks that decide whether this tool may change its membership.</summary>
    public sealed class GroupInfo
    {
        /// <summary>The group's object ID.</summary>
        public string Id { get; private set; }

        /// <summary>The group's display name.</summary>
        public string DisplayName { get; private set; }

        /// <summary>The group's email address, or null when it has none.</summary>
        public string Mail { get; private set; }

        /// <summary>The Graph groupTypes values, such as Unified or DynamicMembership.</summary>
        public IList<string> GroupTypes { get; private set; }

        /// <summary>The dynamic membership rule, or null when members are added by hand.</summary>
        public string MembershipRule { get; private set; }

        /// <summary>True when directory roles can be assigned to the group.</summary>
        public bool IsAssignableToRole { get; private set; }

        /// <summary>True when the group can receive email.</summary>
        public bool MailEnabled { get; private set; }

        /// <summary>True when the group is a security group.</summary>
        public bool SecurityEnabled { get; private set; }

        /// <summary>True when the group is synchronised from on-premises Active Directory.</summary>
        public bool OnPremisesSyncEnabled { get; private set; }

        /// <summary>True when membership is set by a rule rather than by hand.</summary>
        public bool IsDynamic => HasType("DynamicMembership") || !string.IsNullOrEmpty(MembershipRule);

        /// <summary>True for a Microsoft 365 group.</summary>
        public bool IsUnified => HasType("Unified");

        /// <summary>A friendly type name: Microsoft 365, Mail-enabled security, Security or Distribution.</summary>
        public string Kind => IsUnified ? "Microsoft 365"
            : SecurityEnabled && MailEnabled ? "Mail-enabled security"
            : SecurityEnabled ? "Security"
            : "Distribution";

        /// <summary>Creates a GroupInfo from a group object returned by Graph.</summary>
        /// <param name="json">The Graph group, read with the properties in DirectoryService.GroupSelect.</param>
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

        /// <summary>Converts the group to the JSON shape sent back to the agent, including whether it is protected and changeable.</summary>
        /// <param name="config">The settings used to work out the protected and changeable flags.</param>
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
