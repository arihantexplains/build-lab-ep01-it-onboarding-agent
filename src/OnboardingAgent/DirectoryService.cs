using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace OnboardingAgent
{
    /// <summary>
    /// Graph operations shared by several functions. It does no scope checks; callers go through <see cref="Validator"/> first.
    /// </summary>
    public sealed class DirectoryService
    {
        public const string UserSelect = "id,displayName,userPrincipalName,mail,accountEnabled,usageLocation,department,jobTitle";
        public const string GroupSelect = "id,displayName,mail,groupTypes,membershipRule,isAssignableToRole,mailEnabled,securityEnabled,onPremisesSyncEnabled";

        private readonly GraphClient _graph;
        private readonly Config _config;

        public DirectoryService(GraphClient graph, Config config)
        {
            _graph = graph;
            _config = config;
        }

        public static string Segment(string value)
        {
            return Uri.EscapeDataString(value);
        }

        public static string Filter(string filter)
        {
            return "$filter=" + Uri.EscapeDataString(filter);
        }

        /// <summary>An OData string literal with embedded quotes doubled.</summary>
        public static string Literal(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
        }

        public UserInfo GetUser(string idOrUpn, bool retryOnReplicationDelay = false)
        {
            if (string.IsNullOrWhiteSpace(idOrUpn))
            {
                throw new ValidationException("A user ID or UPN is required.");
            }

            idOrUpn = idOrUpn.Trim();
            Guid ignored;
            if (idOrUpn.IndexOf('@') < 0 && !Guid.TryParse(idOrUpn, out ignored))
            {
                return FindUserByDisplayName(idOrUpn);
            }

            var json = _graph.Get("/users/" + Segment(idOrUpn) + "?$select=" + UserSelect, new GraphCallOptions
            {
                RetryOnReplicationDelay = retryOnReplicationDelay,
                NotFoundMessage = "User '" + idOrUpn + "' was not found."
            });
            return UserInfo.From(json);
        }

        /// <summary>
        /// Names typed in chat ("Ankit Verma") resolve to the prefixed account ("Demo-Ankit Verma"); the name must match exactly one user.
        /// </summary>
        private UserInfo FindUserByDisplayName(string name)
        {
            var candidates = name.StartsWith(_config.RequiredNamePrefix, StringComparison.Ordinal)
                ? new[] { name }
                : new[] { _config.RequiredNamePrefix + name, name };
            foreach (var candidate in candidates)
            {
                var matches = _graph.Get("/users?" + Filter("displayName eq " + Literal(candidate)) + "&$select=" + UserSelect + "&$top=2")
                    .GetArray("value").Objects().ToList();
                if (matches.Count > 1)
                {
                    throw new ValidationException("More than one user is named '" + candidate + "'. Use the UPN instead.");
                }

                if (matches.Count == 1)
                {
                    return UserInfo.From(matches[0]);
                }
            }

            throw new ValidationException("User '" + name + "' was not found.");
        }

        public bool UserExists(string userPrincipalName)
        {
            try
            {
                _graph.Get("/users/" + Segment(userPrincipalName) + "?$select=id");
                return true;
            }
            catch (GraphException ex) when (ex.StatusCode == 404)
            {
                return false;
            }
        }

        /// <summary>The user's manager, or null when none is set.</summary>
        public UserInfo GetManager(string userId)
        {
            try
            {
                return UserInfo.From(_graph.Get("/users/" + userId + "/manager?$select=" + UserSelect));
            }
            catch (GraphException ex) when (ex.StatusCode == 404)
            {
                return null;
            }
        }

        public GroupInfo GetGroup(string idOrName)
        {
            if (string.IsNullOrWhiteSpace(idOrName))
            {
                throw new ValidationException("A group ID or name is required.");
            }

            idOrName = idOrName.Trim();
            Guid id;
            if (Guid.TryParse(idOrName, out id))
            {
                return GroupInfo.From(_graph.Get("/groups/" + id.ToString("D") + "?$select=" + GroupSelect, new GraphCallOptions
                {
                    NotFoundMessage = "Group '" + idOrName + "' was not found."
                }));
            }

            var matches = _graph.Get("/groups?" + Filter("displayName eq " + Literal(idOrName)) + "&$select=" + GroupSelect + "&$top=2")
                .GetArray("value").Objects().ToList();
            if (matches.Count == 0)
            {
                throw new ValidationException("Group '" + idOrName + "' was not found.");
            }

            if (matches.Count > 1)
            {
                throw new ValidationException("More than one group is named '" + idOrName + "'. Use the group ID instead.");
            }

            return GroupInfo.From(matches[0]);
        }

        public List<GroupInfo> GetUserGroups(string userId, bool transitive)
        {
            bool truncated;
            var path = "/users/" + userId + (transitive ? "/transitiveMemberOf" : "/memberOf") + "/microsoft.graph.group?$select=" + GroupSelect;
            return _graph.GetAll(path, 2000, out truncated).Select(GroupInfo.From).ToList();
        }

        /// <summary>
        /// POST /users needs only User.Create. Creating straight inside the administrative unit would need
        /// AdministrativeUnit.ReadWrite.All (or an AU-scoped admin role, which needs Entra ID P1), which this app does not have.
        /// </summary>
        public UserInfo CreateUser(JsonObject graphUser)
        {
            var created = _graph.Post("/users", graphUser);

            // The POST response only carries default properties, so report what was set from the request (never the password).
            var result = new JsonObject();
            foreach (var pair in graphUser)
            {
                if (pair.Key != "passwordProfile" && pair.Key != "@odata.type")
                {
                    result[pair.Key] = pair.Value;
                }
            }

            result["id"] = created.GetString("id");
            result["mail"] = created.GetString("mail");
            return UserInfo.From(result);
        }

        /// <summary>Adds the user to the managed administrative unit. Fails with 403 unless the app may manage the unit.</summary>
        public void AddToAdminUnit(string userId)
        {
            try
            {
                _graph.Post("/directory/administrativeUnits/" + _config.AdminUnitId + "/members/$ref",
                    new JsonObject { { "@odata.id", GraphClient.GraphRoot + "/directoryObjects/" + userId } },
                    new GraphCallOptions { Idempotent = true, RetryOnReplicationDelay = true });
            }
            catch (GraphException ex) when (ex.IsAlreadyMember)
            {
            }
            catch (GraphException ex) when (ex.StatusCode == 403)
            {
                throw new AdminActionException("The app may not add users to the administrative unit, so an administrator must add this user to it before the tool can change it.");
            }
        }

        /// <summary>Like AddToAdminUnit, but returns false instead of failing when the app may not manage the unit (403).</summary>
        public bool TryAddToAdminUnit(string userId)
        {
            try
            {
                AddToAdminUnit(userId);
                return true;
            }
            catch (AdminActionException)
            {
                return false;
            }
        }

        /// <summary>True when the account exists and has a mailbox address. A missing account counts as no mailbox.</summary>
        public bool HasMailbox(string idOrUpn)
        {
            if (string.IsNullOrWhiteSpace(idOrUpn))
            {
                return false;
            }

            try
            {
                return !string.IsNullOrEmpty(_graph.Get("/users/" + Segment(idOrUpn.Trim()) + "?$select=mail").GetString("mail"));
            }
            catch (GraphException ex) when (ex.StatusCode == 404)
            {
                return false;
            }
        }

        public void SetManager(string userId, string managerId, bool retryOnReplicationDelay)
        {
            _graph.Put("/users/" + userId + "/manager/$ref",
                new JsonObject { { "@odata.id", GraphClient.GraphRoot + "/users/" + managerId } },
                new GraphCallOptions { RetryOnReplicationDelay = retryOnReplicationDelay });
        }

        /// <summary>Returns false when the user was already a member.</summary>
        public bool AddGroupMember(string groupId, string userId, bool retryOnReplicationDelay)
        {
            try
            {
                _graph.Post("/groups/" + groupId + "/members/$ref",
                    new JsonObject { { "@odata.id", GraphClient.GraphRoot + "/directoryObjects/" + userId } },
                    new GraphCallOptions { Idempotent = true, RetryOnReplicationDelay = retryOnReplicationDelay });
                return true;
            }
            catch (GraphException ex) when (ex.IsAlreadyMember)
            {
                return false;
            }
        }

        /// <summary>Returns false when the user was not a member.</summary>
        public bool RemoveGroupMember(string groupId, string userId)
        {
            try
            {
                _graph.Delete("/groups/" + groupId + "/members/" + userId + "/$ref");
                return true;
            }
            catch (GraphException ex) when (ex.StatusCode == 404)
            {
                return false;
            }
        }

        public void SendMail(IEnumerable<string> to, IEnumerable<string> cc, string subject, string body, bool isHtml, bool saveToSentItems)
        {
            var message = new JsonObject
            {
                { "subject", subject },
                { "body", new JsonObject { { "contentType", isHtml ? "HTML" : "Text" }, { "content", body } } },
                { "toRecipients", Recipients(to) }
            };
            if (cc != null && cc.Any())
            {
                message["ccRecipients"] = Recipients(cc);
            }

            _graph.Post("/users/" + Segment(_config.SenderMailbox) + "/sendMail",
                new JsonObject { { "message", message }, { "saveToSentItems", saveToSentItems } },
                new GraphCallOptions { NotFoundMessage = "The sender mailbox " + _config.SenderMailbox + " was not found." });
        }

        public void SendTemporaryPassword(UserInfo recipient, UserInfo account, string password, string reason, bool mustChange, string correlationId)
        {
            if (string.IsNullOrEmpty(recipient.Mail))
            {
                throw new ValidationException(recipient.DisplayName + " has no mailbox, so the temporary password cannot be delivered.");
            }

            var body =
                "<p>Hello " + WebUtility.HtmlEncode(recipient.DisplayName) + ",</p>" +
                "<p>" + WebUtility.HtmlEncode(reason) + " The user is <b>" + WebUtility.HtmlEncode(account.DisplayName) + "</b> (" +
                WebUtility.HtmlEncode(account.UserPrincipalName) + ").</p>" +
                "<p>Temporary password: <code style=\"font-size:14px\">" + WebUtility.HtmlEncode(password) + "</code></p>" +
                "<p>Please pass it on in person or by phone, not by email." +
                (mustChange ? " It must be changed at first sign-in." : string.Empty) + "</p>" +
                "<p style=\"color:#666\">Reference: " + correlationId + "</p>";

            // Kept out of Sent Items so the password does not persist in the shared sender mailbox.
            SendMail(new[] { recipient.Mail }, null, "Sign-in details for " + account.DisplayName, body, true, false);
        }

        private static JsonArray Recipients(IEnumerable<string> addresses)
        {
            return new JsonArray(addresses.Select(address =>
                (object)new JsonObject { { "emailAddress", new JsonObject { { "address", address } } } }));
        }
    }
}
