using System.Collections.Generic;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "groups": [ group IDs or exact display names ] } for AddToGroups and RemoveFromGroups.</summary>
    public sealed class GroupChangeParameters
    {
        private const int MaxGroups = 25;

        /// <summary>The user to add or remove, by object ID or UPN, from "userId". Required.</summary>
        public string UserId { get; private set; }

        /// <summary>Group IDs or exact display names from "groups". Required, 1 to 25 groups. Blank and duplicate entries are dropped.</summary>
        public List<string> Groups { get; private set; }

        /// <summary>Reads and checks the parameters for AddToGroups or RemoveFromGroups.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"userId" is missing, or "groups" is empty or lists more than 25 groups.</exception>
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
