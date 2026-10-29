using System.Collections.Generic;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "groups": [ group IDs or exact display names ] } for AddToGroups and RemoveFromGroups.</summary>
    public sealed class GroupChangeParameters
    {
        private const int MaxGroups = 25;

        public string UserId { get; private set; }

        public List<string> Groups { get; private set; }

        public static GroupChangeParameters Parse(JsonObject p)
        {
            var result = new GroupChangeParameters
            {
                UserId = p.RequireString("userId"),
                Groups = p.GetStringList("groups")
            };

            if (result.Groups.Count == 0)
            {
                throw new ValidationException("Parameter 'groups' must list at least one group.");
            }

            if (result.Groups.Count > MaxGroups)
            {
                throw new ValidationException("At most " + MaxGroups + " groups can be changed in one call.");
            }

            return result;
        }
    }
}
