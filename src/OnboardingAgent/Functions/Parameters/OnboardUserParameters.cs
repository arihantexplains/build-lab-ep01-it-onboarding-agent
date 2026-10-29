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

        public NewUserParameters User { get; private set; }

        public string ManagerId => User.ManagerId;

        public List<string> Licences { get; private set; }

        public List<string> Groups { get; private set; }

        public string CloneFromUserId { get; private set; }

        public bool SendWelcomeEmail { get; private set; }

        public bool PreviewOnly { get; private set; }

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
