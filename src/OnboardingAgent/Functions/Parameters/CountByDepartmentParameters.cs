namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "department": optional filter, "includeDisabled": true by default }</summary>
    public sealed class CountByDepartmentParameters
    {
        /// <summary>Exact department to count, from "department". Optional; null counts every department.</summary>
        public string Department { get; private set; }

        /// <summary>Whether disabled users are counted, from "includeDisabled". Optional, default true.</summary>
        public bool IncludeDisabled { get; private set; }

        /// <summary>Reads and checks the parameters for CountByDepartment.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"includeDisabled" is not true or false.</exception>
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
