namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "nameStartsWith": optional, with or without the required prefix, "top": 1-500, default 100 }</summary>
    public sealed class ListGroupsParameters
    {
        public string NameStartsWith { get; private set; }

        public int Top { get; private set; }

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
