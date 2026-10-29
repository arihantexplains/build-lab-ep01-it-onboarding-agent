using System.Collections.Generic;
using System.Linq;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Builds an .xlsx of in-scope users or a group's in-scope members and saves it to the reports site's document library.
    /// </summary>
    public static class BuildExcelReport
    {
        private const int MaxRows = 10000;

        // Graph's single-request upload accepts up to 4 MB; larger files would need an upload session.
        private const int MaxUploadBytes = 4 * 1024 * 1024;

        private static readonly string[] UserHeaders =
        {
            "Display name", "UPN", "Department", "Job title", "Office", "Enabled", "Created (UTC)", "Usage location"
        };

        private static readonly string[] MemberHeaders = { "Display name", "UPN", "Department", "Job title", "Enabled" };

        /// <summary>Builds the workbook and uploads it to the reports folder. If the file name is taken, the new file is renamed.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: reportType (Users or GroupMembers), department, group, fileName and folder.</param>
        /// <returns>The ActionResult sent back to the agent, with the saved file's name, web link and row count.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = ExcelReportParameters.Parse(parameters);
            var siteId = ctx.Config.RequireReportsSiteId();
            if (!siteId.All(c => char.IsLetterOrDigit(c) || c == '.' || c == ',' || c == '-'))
            {
                throw new AdminActionException("Environment variable " + Config.ReportsSiteIdVariable + " is not a valid Graph site ID.");
            }

            string[] headers;
            List<IList<string>> rows;
            bool truncated;
            var outOfScope = 0;
            JsonObject group = null;

            if (p.ReportType == ExcelReportParameters.UsersReport)
            {
                var filter = "startswith(displayName," + DirectoryService.Literal(ctx.Config.RequiredNamePrefix) + ")";
                if (p.Department != null)
                {
                    filter += " and department eq " + DirectoryService.Literal(p.Department);
                }

                var users = ctx.Graph.GetAll("/users?" + DirectoryService.Filter(filter)
                        + "&$count=true&$top=999&$select=displayName,userPrincipalName,department,jobTitle,officeLocation,accountEnabled,createdDateTime,usageLocation",
                    MaxRows, out truncated, new GraphCallOptions { EventualConsistency = true });

                headers = UserHeaders;
                rows = users
                    .Where(u => ctx.Validator.InScope(u.GetString("displayName")))
                    .Select(u => (IList<string>)new[]
                    {
                        u.GetString("displayName"), u.GetString("userPrincipalName"), u.GetString("department"), u.GetString("jobTitle"),
                        u.GetString("officeLocation"), u.GetBool("accountEnabled", false) ? "Yes" : "No", u.GetString("createdDateTime"),
                        u.GetString("usageLocation")
                    })
                    .ToList();
            }
            else
            {
                var target = ctx.Validator.ReadableGroup(p.Group);
                group = new JsonObject { { "id", target.Id }, { "displayName", target.DisplayName } };

                var members = ctx.Graph.GetAll("/groups/" + target.Id
                        + "/members/microsoft.graph.user?$top=999&$select=displayName,userPrincipalName,department,jobTitle,accountEnabled",
                    MaxRows, out truncated);

                headers = MemberHeaders;
                rows = members
                    .Where(m => ctx.Validator.InScope(m.GetString("displayName")))
                    .Select(m => (IList<string>)new[]
                    {
                        m.GetString("displayName"), m.GetString("userPrincipalName"), m.GetString("department"), m.GetString("jobTitle"),
                        m.GetBool("accountEnabled", false) ? "Yes" : "No"
                    })
                    .ToList();
                outOfScope = members.Count - rows.Count;
            }

            rows = rows.OrderBy(r => r[0]).ToList();
            var workbook = XlsxBuilder.Build(p.ReportType, headers, rows);
            if (workbook.Length > MaxUploadBytes)
            {
                throw new ValidationException("The report is larger than 4 MB. Narrow it, for example with a department filter.");
            }

            // Site IDs ("host,guid,guid") are used as-is; escaping the commas breaks the lookup.
            var item = ctx.Graph.PutBytes("/sites/" + siteId + "/drive/root:/" + p.EscapedFolder + "/" + DirectoryService.Segment(p.FileName)
                    + ":/content?@microsoft.graph.conflictBehavior=rename",
                workbook, XlsxBuilder.ContentType);

            var data = new JsonObject
            {
                { "reportType", p.ReportType },
                { "fileName", item.GetString("name") },
                { "webUrl", item.GetString("webUrl") },
                { "rows", rows.Count },
                { "truncated", truncated },
                { "outOfScopeRowsOmitted", outOfScope }
            };
            if (group != null)
            {
                data["group"] = group;
            }

            return ActionResult.Ok("Saved " + item.GetString("name") + " with " + rows.Count + " row(s)"
                + (truncated ? " (partial: the row limit or time limit was reached)." : "."), data);
        }
    }
}
