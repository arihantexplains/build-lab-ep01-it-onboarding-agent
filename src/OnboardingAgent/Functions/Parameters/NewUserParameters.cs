using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// A new account. The UPN is always placed in the configured domain; when it is omitted it is built as given.surname.
    /// </summary>
    public sealed class NewUserParameters
    {
        private static readonly Regex LocalPartPattern = new Regex(@"^[a-z0-9](?:[a-z0-9._'-]{0,62}[a-z0-9])?$", RegexOptions.CultureInvariant);
        private static readonly Regex NicknameInvalid = new Regex(@"[^a-z0-9._-]", RegexOptions.CultureInvariant);

        public string DisplayName { get; private set; }

        public string GivenName { get; private set; }

        public string Surname { get; private set; }

        public string UserPrincipalName { get; private set; }

        public string MailNickname { get; private set; }

        public string Department { get; private set; }

        public string JobTitle { get; private set; }

        public string OfficeLocation { get; private set; }

        public string CompanyName { get; private set; }

        public string EmployeeId { get; private set; }

        public string MobilePhone { get; private set; }

        public string UsageLocation { get; private set; }

        public string ManagerId { get; private set; }

        public static NewUserParameters Parse(JsonObject p, Config config)
        {
            var result = new NewUserParameters
            {
                DisplayName = p.RequireString("displayName"),
                GivenName = p.RequireString("givenName"),
                Surname = p.RequireString("surname"),
                Department = p.GetTrimmed("department"),
                JobTitle = p.GetTrimmed("jobTitle"),
                OfficeLocation = p.GetTrimmed("officeLocation"),
                CompanyName = p.GetTrimmed("companyName"),
                EmployeeId = p.GetTrimmed("employeeId"),
                MobilePhone = p.GetTrimmed("mobilePhone"),
                UsageLocation = ParseUsageLocation(p.GetTrimmed("usageLocation")),
                ManagerId = p.GetTrimmed("managerId")
            };

            if (result.DisplayName.Length > 256)
            {
                throw new ValidationException("displayName must be 256 characters or fewer.");
            }

            // Default UPN carries the scope prefix too ("Demo-" gives demo-given.surname), so names and UPNs line up.
            var upnPrefix = Simplify(config.RequiredNamePrefix.TrimEnd('-'));
            var localPart = LocalPartFrom(p.GetTrimmed("userPrincipalName"), config)
                ?? (upnPrefix.Length > 0 ? upnPrefix + "-" : string.Empty) + Simplify(result.GivenName) + "." + Simplify(result.Surname);
            if (!LocalPartPattern.IsMatch(localPart) || localPart.Contains(".."))
            {
                throw new ValidationException("'" + localPart + "' is not a valid UPN user name. Pass userPrincipalName explicitly.");
            }

            result.UserPrincipalName = localPart + "@" + config.UpnDomain;

            var nickname = NicknameInvalid.Replace((p.GetTrimmed("mailNickname") ?? localPart).ToLowerInvariant(), string.Empty);
            if (nickname.Length == 0 || nickname.Length > 64)
            {
                throw new ValidationException("mailNickname must contain 1 to 64 letters, digits, dots, hyphens or underscores.");
            }

            result.MailNickname = nickname;
            return result;
        }

        public static string ParseUsageLocation(string value)
        {
            if (value == null)
            {
                return null;
            }

            if (value.Length != 2 || !value.All(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
            {
                throw new ValidationException("usageLocation must be a two-letter country code such as GB or US.");
            }

            return value.ToUpperInvariant();
        }

        /// <summary>The Graph request body. The only place the temporary password is attached.</summary>
        public JsonObject ToGraphUser(string temporaryPassword)
        {
            var user = new JsonObject
            {
                { "@odata.type", "#microsoft.graph.user" },
                { "accountEnabled", true },
                { "displayName", DisplayName },
                { "givenName", GivenName },
                { "surname", Surname },
                { "userPrincipalName", UserPrincipalName },
                { "mailNickname", MailNickname },
                { "passwordProfile", new JsonObject { { "forceChangePasswordNextSignIn", true }, { "password", temporaryPassword } } }
            };
            AddOptional(user);
            return user;
        }

        public JsonObject ToPreview()
        {
            var user = new JsonObject
            {
                { "displayName", DisplayName },
                { "givenName", GivenName },
                { "surname", Surname },
                { "userPrincipalName", UserPrincipalName },
                { "mailNickname", MailNickname }
            };
            AddOptional(user);
            return user;
        }

        private void AddOptional(JsonObject user)
        {
            SetIfPresent(user, "department", Department);
            SetIfPresent(user, "jobTitle", JobTitle);
            SetIfPresent(user, "officeLocation", OfficeLocation);
            SetIfPresent(user, "companyName", CompanyName);
            SetIfPresent(user, "employeeId", EmployeeId);
            SetIfPresent(user, "mobilePhone", MobilePhone);
            SetIfPresent(user, "usageLocation", UsageLocation);
        }

        private static void SetIfPresent(JsonObject target, string key, string value)
        {
            if (value != null)
            {
                target[key] = value;
            }
        }

        private static string LocalPartFrom(string userPrincipalName, Config config)
        {
            if (userPrincipalName == null)
            {
                return null;
            }

            var at = userPrincipalName.IndexOf('@');
            if (at < 0)
            {
                return userPrincipalName.ToLowerInvariant();
            }

            if (!string.Equals(userPrincipalName.Substring(at + 1), config.UpnDomain, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("userPrincipalName must use the domain " + config.UpnDomain + ".");
            }

            return userPrincipalName.Substring(0, at).ToLowerInvariant();
        }

        /// <summary>Strips accents and anything that is not a letter, digit or hyphen, so "Zoë O'Neil" becomes "zoe" and "oneil".</summary>
        private static string Simplify(string name)
        {
            var result = new StringBuilder();
            foreach (var c in name.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-')
                {
                    result.Append(char.ToLowerInvariant(c));
                }
            }

            return result.ToString();
        }
    }
}
