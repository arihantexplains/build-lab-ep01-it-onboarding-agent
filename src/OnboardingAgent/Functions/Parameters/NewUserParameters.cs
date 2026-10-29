using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// A new user. The UPN is always placed in the configured domain; when it is omitted it is built as given.surname.
    /// </summary>
    public sealed class NewUserParameters
    {
        private static readonly Regex LocalPartPattern = new Regex(@"^[a-z0-9](?:[a-z0-9._'-]{0,62}[a-z0-9])?$", RegexOptions.CultureInvariant);
        private static readonly Regex NicknameInvalid = new Regex(@"[^a-z0-9._-]", RegexOptions.CultureInvariant);

        /// <summary>Display name from "displayName". Required, 256 characters or fewer.</summary>
        public string DisplayName { get; private set; }

        /// <summary>First name from "givenName". Required.</summary>
        public string GivenName { get; private set; }

        /// <summary>Last name from "surname". Required.</summary>
        public string Surname { get; private set; }

        /// <summary>Sign-in name in the configured domain. Built from "userPrincipalName" when given (a different domain is refused), otherwise from the name prefix, given name and surname.</summary>
        public string UserPrincipalName { get; private set; }

        /// <summary>Mail alias from "mailNickname", or the UPN user name when omitted. Lower case, 1 to 64 letters, digits, dots, hyphens or underscores; other characters are removed.</summary>
        public string MailNickname { get; private set; }

        /// <summary>Department from "department". Optional.</summary>
        public string Department { get; private set; }

        /// <summary>Job title from "jobTitle". Optional.</summary>
        public string JobTitle { get; private set; }

        /// <summary>Office location from "officeLocation". Optional.</summary>
        public string OfficeLocation { get; private set; }

        /// <summary>Company name from "companyName". Optional.</summary>
        public string CompanyName { get; private set; }

        /// <summary>Employee ID from "employeeId". Optional.</summary>
        public string EmployeeId { get; private set; }

        /// <summary>Mobile phone number from "mobilePhone". Optional.</summary>
        public string MobilePhone { get; private set; }

        /// <summary>Two-letter country code from "usageLocation", in upper case. Optional, but licences cannot be assigned without one.</summary>
        public string UsageLocation { get; private set; }

        /// <summary>The manager, by object ID or UPN, from "managerId". Optional here; OnboardUser requires it.</summary>
        public string ManagerId { get; private set; }

        /// <summary>Reads and checks the parameters for CreateUser and builds the UPN and mail alias.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <param name="config">The plugin settings that give the UPN domain and the required name prefix.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">A required name is missing, the display name is too long, or the UPN, mail alias or usage location is not valid.</exception>
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

        /// <summary>Checks a usage location and returns it in upper case.</summary>
        /// <param name="value">The trimmed value, or null when it was not given.</param>
        /// <returns>The two-letter country code in upper case, or null when <paramref name="value"/> is null.</returns>
        /// <exception cref="ValidationException">The value is not exactly two letters.</exception>
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
        /// <param name="temporaryPassword">The first password. The user must change it at the next sign-in.</param>
        /// <returns>A new JSON object for the Graph create user request, with the user enabled.</returns>
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

        /// <summary>The fields that would be sent to Graph, without the password, for a preview.</summary>
        /// <returns>A new JSON object with the names, UPN, mail alias and any optional fields that are set.</returns>
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
