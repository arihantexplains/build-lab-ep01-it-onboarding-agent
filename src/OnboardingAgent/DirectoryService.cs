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
        /// <summary>User properties requested for every user lookup.</summary>
        public const string UserSelect = "id,displayName,userPrincipalName,mail,accountEnabled,usageLocation,department,jobTitle";
        /// <summary>Group properties requested for every group lookup, including those the write checks need.</summary>
        public const string GroupSelect = "id,displayName,mail,groupTypes,membershipRule,isAssignableToRole,mailEnabled,securityEnabled,onPremisesSyncEnabled";

        private readonly GraphClient _graph;
        private readonly Config _config;

        /// <summary>Creates the service over a Graph client and the loaded settings.</summary>
        /// <param name="graph">Client for Microsoft Graph calls.</param>
        /// <param name="config">Settings loaded from environment variables.</param>
        public DirectoryService(GraphClient graph, Config config)
        {
            _graph = graph;
            _config = config;
        }

        /// <summary>Escapes a value for use as one segment of a Graph URL path.</summary>
        public static string Segment(string value)
        {
            return Uri.EscapeDataString(value);
        }

        /// <summary>Builds an escaped $filter query option.</summary>
        /// <param name="filter">The OData filter expression.</param>
        public static string Filter(string filter)
        {
            return "$filter=" + Uri.EscapeDataString(filter);
        }

        /// <summary>An OData string literal with embedded quotes doubled.</summary>
        public static string Literal(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
        }

        /// <summary>Finds a user by object ID, UPN or display name. A bare name is matched with and without the required prefix.</summary>
        /// <param name="idOrUpn">Object ID, UPN or display name.</param>
        /// <param name="retryOnReplicationDelay">Retry a 404 briefly, for a user created moments ago.</param>
        /// <returns>The user. Throws when not found or when a name matches more than one user.</returns>
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
        /// Names typed in chat ("Ankit Verma") resolve to the prefixed user ("Demo-Ankit Verma"); the name must match exactly one user.
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

        /// <summary>True when a user with this UPN or ID exists, used to avoid duplicate UPNs.</summary>
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

        /// <summary>Finds a group by object ID or exact display name.</summary>
        /// <param name="idOrName">Group object ID or display name.</param>
        /// <returns>The group. Throws when not found or when the name matches more than one group.</returns>
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

        /// <summary>Lists the groups a user belongs to, up to 2000.</summary>
        /// <param name="userId">The user's object ID.</param>
        /// <param name="transitive">True to include groups reached through nested membership.</param>
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

        /// <summary>True when the user exists and has a mailbox address. A missing user counts as no mailbox.</summary>
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

        /// <summary>Sets or replaces the user's manager.</summary>
        /// <param name="userId">The user's object ID.</param>
        /// <param name="managerId">The manager's object ID.</param>
        /// <param name="retryOnReplicationDelay">Retry briefly when either user was created moments ago.</param>
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

        /// <summary>Sends an email from the configured sender mailbox. Recipients are not checked here; callers use <see cref="Validator.RequireInternalRecipient"/> first.</summary>
        /// <param name="to">Recipient addresses.</param>
        /// <param name="cc">Copy addresses, or null.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="body">Email body.</param>
        /// <param name="isHtml">True when the body is HTML, false for plain text.</param>
        /// <param name="saveToSentItems">Whether to keep a copy in the sender's Sent Items.</param>
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

        /// <summary>Emails a temporary password to a person such as the manager, without saving a copy in Sent Items.</summary>
        /// <param name="recipient">Who receives the email. Must have a mailbox.</param>
        /// <param name="account">The user the password belongs to.</param>
        /// <param name="password">The temporary password.</param>
        /// <param name="reason">Opening sentence explaining why the password was set.</param>
        /// <param name="mustChange">Adds a note that the password must be changed at first sign-in.</param>
        /// <param name="correlationId">Reference shown at the bottom of the email.</param>
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
