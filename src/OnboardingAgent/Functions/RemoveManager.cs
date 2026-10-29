using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Clears the user's manager (for example before a reorganisation). Use SetManager to set a new one.
    /// </summary>
    public static class RemoveManager
    {
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = UserTargetParameters.Parse(parameters);
            var user = ctx.Validator.WritableUser(p.UserId);
            var data = new JsonObject { { "user", user.ToJson() } };

            var manager = ctx.Directory.GetManager(user.Id);
            if (manager == null)
            {
                return ActionResult.Ok(user.DisplayName + " has no manager, so nothing was changed.", data);
            }

            try
            {
                ctx.Graph.Delete("/users/" + user.Id + "/manager/$ref");
            }
            catch (GraphException ex) when (ex.StatusCode == 404)
            {
                return ActionResult.Ok(user.DisplayName + " has no manager, so nothing was changed.", data);
            }

            data["previousManager"] = ctx.Validator.InScope(manager.DisplayName) ? manager.DisplayName : "(outside managed scope)";
            return ActionResult.Ok("Removed the manager of " + user.DisplayName + ".", data);
        }
    }
}
