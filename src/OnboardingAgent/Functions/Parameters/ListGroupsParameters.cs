namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "nameStartsWith": optional, with or without the required prefix, "top": 1-500, default 100 }</summary>
    public sealed class ListGroupsParameters
    {
        /// <summary>Start of the group name to match, from "nameStartsWith". Optional; the required name prefix is added when it is missing.</summary>
        public string NameStartsWith { get; private set; }

        /// <summary>Maximum number of groups to return, from "top". Optional, 1 to 500, default 100.</summary>
        public int Top { get; private set; }

        /// <summary>Reads and checks the parameters for ListGroups.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"top" is not a whole number from 1 to 500.</exception>
        public static ListGroupsParameters Parse(JsonObject p)
        {
            return new ListGroupsParameters
            {
                NameStartsWith = p.GetTrimmed("nameStartsWith"),
                Top = p.GetInt("top", 100, 1, 500)
            };
        }
    }
}
