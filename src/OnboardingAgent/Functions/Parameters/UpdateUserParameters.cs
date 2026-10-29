using System;
using System.Linq;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// { "userId": ..., then any of the allowed fields }. A field set to null or "" is cleared.
    /// Sign-in name, sign-in state and passwords have their own actions and are refused here.
    /// </summary>
    public sealed class UpdateUserParameters
    {
        private static readonly string[] TextFields =
        {
            "givenName", "surname", "displayName", "jobTitle", "department", "officeLocation", "companyName", "employeeId",
            "employeeType", "mobilePhone", "usageLocation", "city", "state", "country", "postalCode", "streetAddress"
        };

        /// <summary>The user to change, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>The Graph PATCH body.</summary>
        public JsonObject Changes { get; private set; }

        /// <summary>Reads and checks the parameters for UpdateUser.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" is missing, no field is given, a field is not allowed or not text, displayName is cleared, or usageLocation is not valid.</exception>
        public static UpdateUserParameters Parse(JsonObject p)
        {
            var changes = new JsonObject();
            foreach (var pair in p)
            {
                if (pair.Key.Equals("userId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (pair.Key.Equals("businessPhones", StringComparison.OrdinalIgnoreCase))
                {
                    changes["businessPhones"] = new JsonArray(p.GetStringList(pair.Key));
                    continue;
                }

                var field = TextFields.FirstOrDefault(name => name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase));
                if (field == null)
                {
                    throw new ValidationException("Field '" + pair.Key + "' cannot be changed with UpdateUser. Allowed fields: "
                        + string.Join(", ", TextFields) + ", businessPhones.");
                }

                if (pair.Value != null && !(pair.Value is string))
                {
                    throw new ValidationException("Field '" + field + "' must be text.");
                }

                var value = ((string)pair.Value)?.Trim();
                changes[field] = string.IsNullOrEmpty(value) ? null : value;
            }

            if (changes.Count == 0)
            {
                throw new ValidationException("Provide at least one field to change.");
            }

            if (changes.ContainsKey("displayName") && changes["displayName"] == null)
            {
                throw new ValidationException("displayName cannot be cleared.");
            }

            if (changes.ContainsKey("usageLocation") && changes["usageLocation"] != null)
            {
                changes["usageLocation"] = NewUserParameters.ParseUsageLocation((string)changes["usageLocation"]);
            }

            return new UpdateUserParameters { UserId = p.RequireString("userId"), Changes = changes };
        }
    }
}
