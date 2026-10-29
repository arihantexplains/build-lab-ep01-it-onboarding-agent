using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    public static class SetManager
    {
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = SetManagerParameters.Parse(parameters);
            var user = ctx.Validator.WritableUser(p.UserId);
            var manager = ctx.Validator.ReadableUser(p.ManagerId);
            if (user.Id == manager.Id)
            {
                throw new ValidationException("A user cannot be their own manager.");
            }

            ctx.Directory.SetManager(user.Id, manager.Id, false);

            return ActionResult.Ok("Set " + manager.DisplayName + " as manager of " + user.DisplayName + ".", new JsonObject
            {
                { "user", user.ToJson() },
                { "manager", manager.ToJson() }
            });
        }
    }
}
