using System;
using System.Globalization;
using System.Linq;

namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>
    /// { "reportType": "Users" (default) or "GroupMembers", "department": optional filter for Users,
    ///   "group": ID or name for GroupMembers, "fileName": optional, "folder": default "Admin Reports" }
    /// </summary>
    public sealed class ExcelReportParameters
    {
        public const string UsersReport = "Users";
        public const string GroupMembersReport = "GroupMembers";

        private static readonly char[] InvalidNameChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|', '#', '%' };

        public string ReportType { get; private set; }

        public string Department { get; private set; }

        public string Group { get; private set; }

        public string FileName { get; private set; }

        /// <summary>Folder path with each segment URL-escaped, ready to put in a Graph drive path.</summary>
        public string EscapedFolder { get; private set; }

        public static ExcelReportParameters Parse(JsonObject p)
        {
            var type = p.GetTrimmed("reportType") ?? UsersReport;
            if (type.Equals(UsersReport, StringComparison.OrdinalIgnoreCase))
            {
                type = UsersReport;
            }
            else if (type.Equals(GroupMembersReport, StringComparison.OrdinalIgnoreCase))
            {
                type = GroupMembersReport;
            }
            else
            {
                throw new ValidationException("reportType must be '" + UsersReport + "' or '" + GroupMembersReport + "'.");
            }

            var result = new ExcelReportParameters
            {
                ReportType = type,
                Department = p.GetTrimmed("department"),
                Group = p.GetTrimmed("group") ?? p.GetTrimmed("groupId"),
                FileName = SafeFileName(p.GetTrimmed("fileName")
                    ?? type + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".xlsx"),
                EscapedFolder = SafeFolder(p.GetTrimmed("folder") ?? "Admin Reports")
            };

            if (type == GroupMembersReport && result.Group == null)
            {
                throw new ValidationException("Parameter 'group' is required for a GroupMembers report.");
            }

            return result;
        }

        private static string SafeFileName(string name)
        {
            name = new string(name.Where(c => Array.IndexOf(InvalidNameChars, c) < 0 && !char.IsControl(c)).ToArray()).Trim();
            if (!name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                name += ".xlsx";
            }

            if (name.Length <= ".xlsx".Length || name.Length > 120)
            {
                throw new ValidationException("fileName must be 1 to 115 characters before .xlsx.");
            }

            return name;
        }

        private static string SafeFolder(string folder)
        {
            var segments = folder.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            if (segments.Count == 0 || segments.Any(s => s.Length == 0 || s == "." || s == ".." || s.IndexOfAny(InvalidNameChars) >= 0))
            {
                throw new ValidationException("folder is not a valid folder path.");
            }

            return string.Join("/", segments.Select(Uri.EscapeDataString));
        }
    }
}
