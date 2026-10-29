using System;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace OnboardingAgent
{
    /// <summary>
    /// Scope and safety rules. Reads need the name prefix; writes also need administrative unit membership and no admin role.
    /// </summary>
    public sealed class Validator
    {
        private readonly Config _config;
        private readonly GraphClient _graph;
        private readonly DirectoryService _directory;
        private readonly ITracingService _trace;

        public Validator(Config config, GraphClient graph, DirectoryService directory, ITracingService trace)
        {
            _config = config;
            _graph = graph;
            _directory = directory;
            _trace = trace;
        }

        public static void RequireApproval(string approvalId, string actionName)
        {
            if (string.IsNullOrWhiteSpace(approvalId))
            {
                throw new ValidationException(actionName + " needs an ApprovalId.");
            }

            if (approvalId.Length > 100 || approvalId.Any(char.IsControl))
            {
                throw new ValidationException("ApprovalId is not valid.");
            }
        }

        public bool InScope(string displayName)
        {
            return _config.HasRequiredPrefix(displayName);
        }

        public void RequirePrefix(string displayName, string kind, string identifier)
        {
            if (!InScope(displayName))
            {
                throw new ValidationException(kind + " '" + identifier + "' is outside the managed scope: display names must start with '"
                    + _config.RequiredNamePrefix + "'.");
            }
        }

        public UserInfo ReadableUser(string idOrUpn)
        {
            var user = _directory.GetUser(idOrUpn);
            RequirePrefix(user.DisplayName, "User", user.UserPrincipalName ?? idOrUpn);
            return user;
        }

        public UserInfo WritableUser(string idOrUpn)
        {
            var user = ReadableUser(idOrUpn);
            RequireInAdminUnit(user);
            RequireNotAdministrator(user);
            return user;
        }

        public GroupInfo ReadableGroup(string idOrName)
        {
            var group = _directory.GetGroup(idOrName);
            RequirePrefix(group.DisplayName, "Group", group.DisplayName ?? idOrName);
            return group;
        }

        public GroupInfo WritableGroup(string idOrName)
        {
            var group = ReadableGroup(idOrName);
            var reason = group.GetWriteBlockReason(_config);
            if (reason != null)
            {
                throw new ValidationException("Group '" + group.DisplayName + "' cannot be changed by this tool because " + reason + ".");
            }

            return group;
        }

        public void RequireInAdminUnit(UserInfo user)
        {
            try
            {
                _graph.Get("/directory/administrativeUnits/" + _config.AdminUnitId + "/members/" + user.Id);
            }
            catch (GraphException ex) when (ex.StatusCode == 404)
            {
                throw new ValidationException("User '" + user.UserPrincipalName + "' is not in the managed administrative unit, so it cannot be changed.");
            }
        }

        /// <summary>
        /// Refuses users with an active or eligible directory role, directly or through a role-assignable group, at any scope.
        /// Graph errors propagate, so the check fails closed.
        /// </summary>
        public void RequireNotAdministrator(UserInfo user)
        {
            var principalFilter = DirectoryService.Filter("principalId eq " + DirectoryService.Literal(user.Id));
            var isAdmin =
                HasAny("/users/" + user.Id + "/transitiveMemberOf/microsoft.graph.directoryRole?$select=id", false)
                || HasAny("/roleManagement/directory/roleAssignments?" + principalFilter + "&$select=id", false)
                || HasAny("/users/" + user.Id + "/transitiveMemberOf/microsoft.graph.group?$count=true&"
                    + DirectoryService.Filter("isAssignableToRole eq true") + "&$select=id", true)
                || HasEligibleRole(principalFilter);

            if (isAdmin)
            {
                throw new ValidationException("'" + user.UserPrincipalName + "' holds or is eligible for an administrator role, so this tool will not change it.");
            }
        }

        public void RequireMailbox(UserInfo user)
        {
            if (string.IsNullOrEmpty(user.Mail))
            {
                throw new ValidationException(user.DisplayName + " has no mailbox, so the temporary password cannot be emailed to them.");
            }
        }

        public void RequireInternalRecipient(string address)
        {
            var at = address.LastIndexOf('@');
            if (at <= 0 || at == address.Length - 1 || address.Any(char.IsWhiteSpace))
            {
                throw new ValidationException("'" + address + "' is not a valid email address.");
            }

            var domain = address.Substring(at + 1);
            if (!domain.Equals(_config.UpnDomain, StringComparison.OrdinalIgnoreCase)
                && !domain.EndsWith("." + _config.UpnDomain, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Email can only be sent to addresses in " + _config.UpnDomain + "; '" + address + "' is not.");
            }
        }

        private bool HasEligibleRole(string principalFilter)
        {
            try
            {
                return HasAny("/roleManagement/directory/roleEligibilityScheduleInstances?" + principalFilter, false);
            }
            catch (GraphException ex) when ((ex.StatusCode == 400 || ex.StatusCode == 403) && MentionsPremiumLicence(ex))
            {
                // Without Entra ID P2 there is no PIM, so nobody can hold an eligible assignment.
                _trace.Trace("PIM eligibility check skipped: {0}.", ex.Code);
                return false;
            }
        }

        private static bool MentionsPremiumLicence(GraphException ex)
        {
            var text = (ex.Code + " " + ex.GraphMessage).ToLowerInvariant();
            return text.Contains("premium") || text.Contains("licen");
        }

        private bool HasAny(string path, bool eventualConsistency)
        {
            return _graph.Get(path, new GraphCallOptions { EventualConsistency = eventualConsistency }).GetArray("value").Count > 0;
        }
    }
}
