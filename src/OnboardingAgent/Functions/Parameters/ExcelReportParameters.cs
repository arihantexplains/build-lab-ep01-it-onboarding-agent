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
        /// <summary>Report type that lists users.</summary>
        public const string UsersReport = "Users";
        /// <summary>Report type that lists the members of one group.</summary>
        public const string GroupMembersReport = "GroupMembers";

        private static readonly char[] InvalidNameChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|', '#', '%' };

        /// <summary>Either <see cref="UsersReport"/> or <see cref="GroupMembersReport"/>, from "reportType" (any letter case). Optional, default Users.</summary>
        public string ReportType { get; private set; }

        /// <summary>Exact department to filter a Users report by, from "department". Optional.</summary>
        public string Department { get; private set; }

        /// <summary>Group ID or name for a GroupMembers report, from "group" (or "groupId"). Required for GroupMembers, otherwise optional.</summary>
        public string Group { get; private set; }

        /// <summary>Workbook file name from "fileName", with characters not allowed in file names removed and ".xlsx" added if missing. Optional, default is the report type plus a UTC time stamp. 1 to 115 characters before ".xlsx".</summary>
        public string FileName { get; private set; }

        /// <summary>Folder path with each segment URL-escaped, ready to put in a Graph drive path.</summary>
        public string EscapedFolder { get; private set; }

        /// <summary>Reads and checks the parameters for BuildExcelReport.</summary>
        /// <param name="p">The action parameters as a JSON object.</param>
        /// <returns>The checked parameters.</returns>
        /// <exception cref="ValidationException">"reportType" is not known, "group" is missing for a GroupMembers report, or the file name or folder is not valid.</exception>
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
