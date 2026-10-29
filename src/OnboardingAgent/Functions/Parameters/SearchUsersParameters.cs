namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// { "name": part of the display name, "department": exact department, "jobTitle": part of the job title,
    ///   "includeDisabled": false by default, "top": 1-200, default 25 }. At least one of name, department or jobTitle.
    /// </summary>
    public sealed class SearchUsersParameters
    {
        public string Name { get; private set; }

        public string Department { get; private set; }

        public string JobTitle { get; private set; }

        public bool IncludeDisabled { get; private set; }

        public int Top { get; private set; }

        public static SearchUsersParameters Parse(JsonObject p)
        {
            var result = new SearchUsersParameters
            {
                Name = p.GetTrimmed(p.ResolveKey("name", "displayName")),
                Department = p.GetTrimmed("department"),
                JobTitle = p.GetTrimmed("jobTitle"),
                IncludeDisabled = p.GetBool("includeDisabled", false),
                Top = p.GetInt("top", 25, 1, 200)
            };

            if (result.Name == null && result.Department == null && result.JobTitle == null)
            {
                throw new ValidationException("Give at least one of 'name', 'department' or 'jobTitle'.");
            }

            if ((result.Name ?? string.Empty).Length > 100 || (result.JobTitle ?? string.Empty).Length > 100 || (result.Department ?? string.Empty).Length > 100)
            {
                throw new ValidationException("Search values must be 100 characters or fewer.");
            }

            return result;
        }
    }
}
