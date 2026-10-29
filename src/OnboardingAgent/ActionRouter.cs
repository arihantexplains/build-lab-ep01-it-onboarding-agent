using System;
using System.Collections.Generic;
using System.Linq;
using OnboardingAgent.Functions;

namespace OnboardingAgent
{
    public static class ActionRouter
    {
        private static readonly Dictionary<string, Func<ActionContext, JsonObject, ActionResult>> Actions =
            new Dictionary<string, Func<ActionContext, JsonObject, ActionResult>>(StringComparer.OrdinalIgnoreCase)
            {
                { "OnboardUser", OnboardUser.Run },
                { "CreateUser", CreateUser.Run },
                { "UpdateUser", UpdateUser.Run },
                { "SetManager", SetManager.Run },
                { "GetUser", GetUser.Run },
                { "CountByDepartment", CountByDepartment.Run },
                { "GetManagerChain", GetManagerChain.Run },
                { "ListGroups", ListGroups.Run },
                { "GetGroupMemberships", GetGroupMemberships.Run },
                { "AddToGroups", AddToGroups.Run },
                { "RemoveFromGroups", RemoveFromGroups.Run },
                { "CloneAccess", CloneAccess.Run },
                { "AssignRole", AssignRole.Run },
                { "GetSignInMethods", GetSignInMethods.Run },
                { "GetSignInStatus", GetSignInStatus.Run },
                { "AuditQuery", AuditQuery.Run },
                { "ResetPassword", ResetPassword.Run },
                { "RevokeSessions", RevokeSessions.Run },
                { "DisableUser", DisableUser.Run },
                { "AssignLicence", AssignLicence.Run },
                { "SendEmail", SendEmail.Run },
                { "BuildExcelReport", BuildExcelReport.Run },
                { "OffboardUser", OffboardUser.Run },
                { "EnableUser", EnableUser.Run },
                { "GetDirectReports", GetDirectReports.Run },
                { "ListGroupMembers", ListGroupMembers.Run },
                { "SearchUsers", SearchUsers.Run },
                { "RemoveManager", RemoveManager.Run },
                { "GetUserLicences", GetUserLicences.Run },
                { "RemoveLicence", RemoveLicence.Run },
                { "ListAssignableRoles", ListAssignableRoles.Run },
                { "GetUserRoles", GetUserRoles.Run },
                { "RemoveRole", RemoveRole.Run }
            };

        private static readonly HashSet<string> ApprovalRequired = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ResetPassword",
            "AssignRole",
            "RemoveRole",
            "RevokeSessions",
            "DisableUser",
            "OffboardUser"
        };

        public static IEnumerable<string> ActionNames => Actions.Keys.OrderBy(name => name, StringComparer.Ordinal);

        public static bool TryGet(string actionName, out Func<ActionContext, JsonObject, ActionResult> handler)
        {
            return Actions.TryGetValue(actionName ?? string.Empty, out handler);
        }

        public static bool RequiresApproval(string actionName)
        {
            return ApprovalRequired.Contains(actionName);
        }
    }
}
