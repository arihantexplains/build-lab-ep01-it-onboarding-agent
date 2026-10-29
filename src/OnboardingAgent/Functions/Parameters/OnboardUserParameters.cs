using System.Collections.Generic;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// New-user fields (see <see cref="NewUserParameters"/>) plus managerId (required), licences, groups or
    /// cloneFromUserId, sendWelcomeEmail (default true) and previewOnly (default true).
    /// </summary>
    public sealed class OnboardUserParameters
    {
        private const int MaxGroups = 25;

        /// <summary>The new user, read from the same JSON with <see cref="NewUserParameters.Parse"/>.</summary>
        public NewUserParameters User { get; private set; }

        /// <summary>The manager from "managerId". Required: the manager receives the welcome email with the temporary password.</summary>
        public string ManagerId => User.ManagerId;

        /// <summary>SKU part numbers or SKU IDs from "licences" (also "licenses" or "skus"). Optional; when any are given, "usageLocation" is required.</summary>
        public List<string> Licences { get; private set; }

        /// <summary>Group IDs or exact display names from "groups". Optional, at most 25. Cannot be used with <see cref="CloneFromUserId"/>.</summary>
        public List<string> Groups { get; private set; }

        /// <summary>A user whose groups are copied, by object ID or UPN, from "cloneFromUserId". Optional. Cannot be used with <see cref="Groups"/>.</summary>
        public string CloneFromUserId { get; private set; }

        /// <summary>Whether to send the welcome email to the manager, from "sendWelcomeEmail". Optional, default true.</summary>
        public bool SendWelcomeEmail { get; private set; }

        /// <summary>When true, only shows what would be done, from "previewOnly". Optional, default true.</summary>
        public bool PreviewOnly { get; private set; }

        /// <summary>Reads and checks the parameters for OnboardUser.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <param name="config">The plugin settings that give the UPN domain and the required name prefix.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">A new user field is not valid, "managerId" is missing, both groups and cloneFromUserId are given, more than 25 groups are listed, or licences are given without a usage location.</exception>
        public static OnboardUserParameters Parse(JsonObject p, Config config)
        {
            var result = new OnboardUserParameters
            {
                User = NewUserParameters.Parse(p, config),
                Licences = p.GetStringList(p.ResolveKey("licences", "licenses", "skus")),
                Groups = p.GetStringList("groups"),
                CloneFromUserId = p.GetTrimmed("cloneFromUserId"),
                SendWelcomeEmail = p.GetBool("sendWelcomeEmail", true),
                PreviewOnly = p.GetBool("previewOnly", true)
            };

            if (result.ManagerId == null)
            {
                throw new ValidationException("Parameter 'managerId' is required: the manager receives the welcome email with the temporary password.");
            }

            if (result.Groups.Count > 0 && result.CloneFromUserId != null)
            {
                throw new ValidationException("Use either groups or cloneFromUserId, not both.");
            }

            if (result.Groups.Count > MaxGroups)
            {
                throw new ValidationException("At most " + MaxGroups + " groups can be added in one call.");
            }

            if (result.Licences.Count > 0 && result.User.UsageLocation == null)
            {
                throw new ValidationException("usageLocation is required when licences are assigned.");
            }

            return result;
        }
    }
}
