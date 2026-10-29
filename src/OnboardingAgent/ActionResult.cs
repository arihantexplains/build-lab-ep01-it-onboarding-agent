namespace OnboardingAgent
{
    public sealed class ActionResult
    {
        private ActionResult(bool success, string message, JsonObject data)
        {
            Success = success;
            Message = message;
            Data = data ?? new JsonObject();
        }

        public bool Success { get; }

        public string Message { get; }

        public JsonObject Data { get; }

        public static ActionResult Ok(string message, JsonObject data = null)
        {
            return new ActionResult(true, message, data);
        }

        public static ActionResult Fail(string message, JsonObject data = null)
        {
            return new ActionResult(false, message, data);
        }

        /// <summary>Success only when every recorded step succeeded.</summary>
        public static ActionResult FromSteps(StepLog steps, string successMessage, string failurePrefix, JsonObject data)
        {
            data["steps"] = steps.Steps;
            return steps.HasFailures
                ? Fail(failurePrefix + " " + steps.FailureSummary, data)
                : Ok(successMessage, data);
        }
    }
}
