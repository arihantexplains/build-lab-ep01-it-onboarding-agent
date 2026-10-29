namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "sourceUserId": ..., "targetUserId": ..., "previewOnly": true by default }</summary>
    public sealed class CloneAccessParameters
    {
        public string SourceUserId { get; private set; }

        public string TargetUserId { get; private set; }

        public bool PreviewOnly { get; private set; }

        public static CloneAccessParameters Parse(JsonObject p)
        {
            return new CloneAccessParameters
            {
                SourceUserId = p.RequireString("sourceUserId"),
                TargetUserId = p.RequireString("targetUserId"),
                PreviewOnly = p.GetBool("previewOnly", true)
            };
        }
    }
}
