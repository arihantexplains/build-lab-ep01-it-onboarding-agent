namespace OnboardingAgent
{
    /// <summary>Outcome of one action: success flag, a message for the caller and a JSON data payload.</summary>
    public sealed class ActionResult
    {
        private ActionResult(bool success, string message, JsonObject data)
        {
            Success = success;
            Message = message;
            Data = data ?? new JsonObject();
        }

        /// <summary>True when the action fully succeeded.</summary>
        public bool Success { get; }

        /// <summary>Short message for the caller describing what happened.</summary>
        public string Message { get; }

        /// <summary>Extra result data. Never null.</summary>
        public JsonObject Data { get; }

        /// <summary>Creates a successful result.</summary>
        /// <param name="message">Message for the caller.</param>
        /// <param name="data">Optional result data.</param>
        public static ActionResult Ok(string message, JsonObject data = null)
        {
            return new ActionResult(true, message, data);
        }

        /// <summary>Creates a failed result.</summary>
        /// <param name="message">Message for the caller explaining the failure.</param>
        /// <param name="data">Optional result data, such as the steps that did complete.</param>
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
