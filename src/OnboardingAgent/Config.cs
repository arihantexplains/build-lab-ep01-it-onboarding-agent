using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace OnboardingAgent
{
    /// <summary>
    /// Settings read from Dataverse environment variables, cached per organization for a few minutes.
    /// </summary>
    public sealed class Config
    {
        public const string TenantIdVariable = "ab_GraphTenantId";
        public const string ClientIdVariable = "ab_GraphClientId";
        public const string ClientSecretVariable = "ab_GraphClientSecret";
        public const string SenderMailboxVariable = "ab_SenderMailbox";
        public const string ReportsSiteIdVariable = "ab_ReportsSiteId";
        public const string AdminUnitIdVariable = "ab_AdminUnitId";
        public const string UpnDomainVariable = "ab_UpnDomain";
        public const string RequiredNamePrefixVariable = "ab_RequiredPrefix";
        public const string AssignableRolesVariable = "ab_AssignableRoles";
        public const string ProtectedGroupsVariable = "ab_ProtectedGroups";

        private const int SecretType = 100000005;
        private const string KeyVaultMarker = "(key vault reference)";

        private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);
        private static readonly object CacheLock = new object();
        private static readonly Dictionary<Guid, Config> Cache = new Dictionary<Guid, Config>();

        private DateTime _loadedUtc;

        private Config()
        {
        }

        public string TenantId { get; private set; }

        public string ClientId { get; private set; }

        public string ClientSecret { get; private set; }

        public string SenderMailbox { get; private set; }

        public string ReportsSiteId { get; private set; }

        public string AdminUnitId { get; private set; }

        public string UpnDomain { get; private set; }

        public string RequiredNamePrefix { get; private set; }

        /// <summary>Role display names, template IDs or definition IDs that AssignRole may grant.</summary>
        public ReadOnlyCollection<string> AssignableRoles { get; private set; }

        /// <summary>Group IDs or display names that are never changed.</summary>
        public ReadOnlyCollection<string> ProtectedGroups { get; private set; }

        public static Config Get(IOrganizationService service, Guid organizationId)
        {
            lock (CacheLock)
            {
                Config cached;
                if (Cache.TryGetValue(organizationId, out cached) && DateTime.UtcNow - cached._loadedUtc < CacheLifetime)
                {
                    return cached;
                }
            }

            var fresh = Load(service);
            lock (CacheLock)
            {
                Cache[organizationId] = fresh;
            }

            return fresh;
        }

        public bool HasRequiredPrefix(string displayName)
        {
            return displayName != null && displayName.StartsWith(RequiredNamePrefix, StringComparison.Ordinal);
        }

        public bool IsProtectedGroup(string id, string displayName)
        {
            return ProtectedGroups.Any(entry =>
                string.Equals(entry, id, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry, displayName, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsAssignableRole(params string[] identifiers)
        {
            return identifiers.Any(identifier => identifier != null
                && AssignableRoles.Contains(identifier, StringComparer.OrdinalIgnoreCase));
        }

        public string RequireReportsSiteId()
        {
            if (string.IsNullOrEmpty(ReportsSiteId))
            {
                throw new AdminActionException("Environment variable " + ReportsSiteIdVariable + " is not set, so reports cannot be saved.");
            }

            return ReportsSiteId;
        }

        private static Config Load(IOrganizationService service)
        {
            var values = ReadValues(service);

            var prefix = Optional(values, RequiredNamePrefixVariable, false);
            if (string.IsNullOrWhiteSpace(prefix))
            {
                // An empty prefix would put every account in scope.
                throw new AdminActionException("Environment variable " + RequiredNamePrefixVariable + " must be set.");
            }

            return new Config
            {
                TenantId = Required(values, TenantIdVariable),
                ClientId = Required(values, ClientIdVariable),
                SenderMailbox = Required(values, SenderMailboxVariable),
                ReportsSiteId = Optional(values, ReportsSiteIdVariable, true),
                AdminUnitId = RequiredGuid(values, AdminUnitIdVariable),
                UpnDomain = Required(values, UpnDomainVariable).TrimStart('@').ToLowerInvariant(),
                RequiredNamePrefix = prefix,
                AssignableRoles = ParseList(Optional(values, AssignableRolesVariable, true), AssignableRolesVariable),
                ProtectedGroups = ParseList(Optional(values, ProtectedGroupsVariable, true), ProtectedGroupsVariable),
                ClientSecret = ReadSecret(service, values),
                _loadedUtc = DateTime.UtcNow
            };
        }

        private static Dictionary<string, string> ReadValues(IOrganizationService service)
        {
            var names = new[]
            {
                TenantIdVariable, ClientIdVariable, ClientSecretVariable, SenderMailboxVariable, ReportsSiteIdVariable, AdminUnitIdVariable,
                UpnDomainVariable, RequiredNamePrefixVariable, AssignableRolesVariable, ProtectedGroupsVariable
            };

            var query = new QueryExpression("environmentvariabledefinition")
            {
                ColumnSet = new ColumnSet("schemaname", "defaultvalue", "type")
            };
            query.Criteria.AddCondition("schemaname", ConditionOperator.In, names.Cast<object>().ToArray());
            var link = query.AddLink("environmentvariablevalue", "environmentvariabledefinitionid", "environmentvariabledefinitionid", JoinOperator.LeftOuter);
            link.EntityAlias = "v";
            link.Columns = new ColumnSet("value");

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in service.RetrieveMultiple(query).Entities)
            {
                var name = row.GetAttributeValue<string>("schemaname");
                var current = row.GetAttributeValue<AliasedValue>("v.value")?.Value as string;
                if (string.Equals(name, ClientSecretVariable, StringComparison.OrdinalIgnoreCase))
                {
                    // A Key Vault secret variable keeps only a reference here; the value comes from RetrieveEnvironmentVariableSecretValue.
                    var type = row.GetAttributeValue<OptionSetValue>("type");
                    if (type != null && type.Value == SecretType)
                    {
                        values[name] = KeyVaultMarker;
                        continue;
                    }
                }

                values[name] = string.IsNullOrEmpty(current) ? row.GetAttributeValue<string>("defaultvalue") : current;
            }

            return values;
        }

        private static string ReadSecret(IOrganizationService service, Dictionary<string, string> values)
        {
            string secret;
            string plain;
            if (values.TryGetValue(ClientSecretVariable, out plain) && plain != KeyVaultMarker)
            {
                // Text variable (no Key Vault in this environment). Read as SYSTEM, kept in memory only, never traced.
                if (string.IsNullOrEmpty(plain))
                {
                    throw new AdminActionException("The environment variable " + ClientSecretVariable + " has no value.");
                }

                return plain;
            }

            try
            {
                var request = new OrganizationRequest("RetrieveEnvironmentVariableSecretValue");
                request["EnvironmentVariableName"] = ClientSecretVariable;
                var response = service.Execute(request);
                secret = response.Results.Contains("EnvironmentVariableSecretValue")
                    ? response.Results["EnvironmentVariableSecretValue"] as string
                    : null;
            }
            catch (Exception ex)
            {
                throw new AdminActionException("Could not read the secret environment variable " + ClientSecretVariable + ". Check that it is a Key Vault secret the environment can access.", ex);
            }

            if (string.IsNullOrEmpty(secret))
            {
                throw new AdminActionException("The secret environment variable " + ClientSecretVariable + " has no value.");
            }

            return secret;
        }

        private static string Optional(Dictionary<string, string> values, string name, bool trim)
        {
            string value;
            if (!values.TryGetValue(name, out value) || string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return trim ? value.Trim() : value;
        }

        private static string Required(Dictionary<string, string> values, string name)
        {
            var value = Optional(values, name, true);
            if (value == null)
            {
                throw new AdminActionException("Environment variable " + name + " must be set.");
            }

            return value;
        }

        private static string RequiredGuid(Dictionary<string, string> values, string name)
        {
            Guid id;
            if (!Guid.TryParse(Required(values, name), out id))
            {
                throw new AdminActionException("Environment variable " + name + " must be a GUID.");
            }

            return id.ToString("D");
        }

        private static ReadOnlyCollection<string> ParseList(string value, string name)
        {
            IEnumerable<string> items = new string[0];
            if (value != null && value.StartsWith("[", StringComparison.Ordinal))
            {
                JsonArray array;
                try
                {
                    array = Json.Parse(value) as JsonArray;
                }
                catch (FormatException)
                {
                    array = null;
                }

                if (array == null)
                {
                    throw new AdminActionException("Environment variable " + name + " is not a valid JSON array.");
                }

                // Entries are strings, or objects such as { "name": "Reports Reader", "templateId": "..." }.
                items = array.SelectMany(entry =>
                {
                    var text = entry as string;
                    if (text != null)
                    {
                        return new[] { text };
                    }

                    var obj = entry as JsonObject;
                    return obj == null
                        ? new string[0]
                        : new[] { obj.GetString("name"), obj.GetString("displayName"), obj.GetString("templateId"), obj.GetString("id") }
                            .Where(item => item != null).ToArray();
                });
            }
            else if (value != null)
            {
                items = value.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            }

            return items
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }
    }
}
