namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "sourceUserId": ..., "targetUserId": ..., "previewOnly": true by default }</summary>
    public sealed class CloneAccessParameters
    {
        /// <summary>The user whose access is copied, by object ID or UPN, from "sourceUserId". Required.</summary>
        public string SourceUserId { get; private set; }

        /// <summary>The user who receives the access, by object ID or UPN, from "targetUserId". Required.</summary>
        public string TargetUserId { get; private set; }

        /// <summary>When true, only shows what would be copied, from "previewOnly". Optional, default true.</summary>
        public bool PreviewOnly { get; private set; }

        /// <summary>Reads and checks the parameters for CloneAccess.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"sourceUserId" or "targetUserId" is missing, or "previewOnly" is not true or false.</exception>
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
