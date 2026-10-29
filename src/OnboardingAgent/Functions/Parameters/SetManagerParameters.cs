namespace OnboardingAgent.Functions.Parameters
{
    /// <summary>{ "userId": ..., "managerId": ... }</summary>
    public sealed class SetManagerParameters
    {
        public string UserId { get; private set; }

        public string ManagerId { get; private set; }

        public static SetManagerParameters Parse(JsonObject p)
        {
            return new SetManagerParameters
            {
                UserId = p.RequireString("userId"),
                ManagerId = p.RequireString("managerId")
            };
        }
    }
}
