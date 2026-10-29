namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "department": optional filter, "includeDisabled": true by default }</summary>
    public sealed class CountByDepartmentParameters
    {
        public string Department { get; private set; }

        public bool IncludeDisabled { get; private set; }

        public static CountByDepartmentParameters Parse(JsonObject p)
        {
            return new CountByDepartmentParameters
            {
                Department = p.GetTrimmed("department"),
                IncludeDisabled = p.GetBool("includeDisabled", true)
            };
        }
    }
}
