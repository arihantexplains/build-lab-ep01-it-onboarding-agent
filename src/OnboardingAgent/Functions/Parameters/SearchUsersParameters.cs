namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// { "name": part of the display name, "department": exact department, "jobTitle": part of the job title,
    ///   "includeDisabled": false by default, "top": 1-200, default 25 }. At least one of name, department or jobTitle.
    /// </summary>
    public sealed class SearchUsersParameters
    {
        /// <summary>Part of the display name to match, from "name" (or "displayName"). Optional, 100 characters or fewer.</summary>
        public string Name { get; private set; }

        /// <summary>Exact department to match, from "department". Optional, 100 characters or fewer.</summary>
        public string Department { get; private set; }

        /// <summary>Part of the job title to match, from "jobTitle". Optional, 100 characters or fewer.</summary>
        public string JobTitle { get; private set; }

        /// <summary>Whether disabled users are included, from "includeDisabled". Optional, default false.</summary>
        public bool IncludeDisabled { get; private set; }

        /// <summary>Maximum number of users to return, from "top". Optional, 1 to 200, default 25.</summary>
        public int Top { get; private set; }

        /// <summary>Reads and checks the parameters for SearchUsers.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">None of name, department or jobTitle is given, a search value is longer than 100 characters, or another value is not valid.</exception>
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
