using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>Returns a managed user's profile details and their manager. A manager outside the managed scope is not named.</summary>
    public static class GetUser
    {
        private const string Select = "id,displayName,givenName,surname,userPrincipalName,mail,jobTitle,department,officeLocation,"
            + "companyName,employeeId,usageLocation,accountEnabled,createdDateTime,userType,businessPhones,mobilePhone";

        /// <summary>Reads the user's profile and manager.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId.</param>
        /// <returns>The ActionResult sent back to the agent, with the user's details and manager.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = UserTargetParameters.Parse(parameters);
            var user = ctx.Validator.ReadableUser(p.UserId);

            var details = ctx.Graph.Get("/users/" + user.Id + "?$select=" + Select);
            details.Remove("@odata.context");

            var manager = ctx.Directory.GetManager(user.Id);
            if (manager == null)
            {
                details["manager"] = null;
            }
            else if (ctx.Validator.InScope(manager.DisplayName))
            {
                details["manager"] = new JsonObject
                {
                    { "id", manager.Id },
                    { "displayName", manager.DisplayName },
                    { "userPrincipalName", manager.UserPrincipalName }
                };
            }
            else
            {
                details["manager"] = new JsonObject { { "outsideManagedScope", true } };
            }

            return ActionResult.Ok("Found " + user.DisplayName + ".", new JsonObject { { "user", details } });
        }
    }
}
