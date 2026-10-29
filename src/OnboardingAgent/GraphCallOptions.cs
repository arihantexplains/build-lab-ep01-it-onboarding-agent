namespace OnboardingAgent
{
    public sealed class GraphCallOptions
    {
        /// <summary>Retry 404 and "reference not present" errors briefly, for objects created moments ago.</summary>
        public bool RetryOnReplicationDelay { get; set; }

        /// <summary>Whether 5xx and network failures may be retried. Defaults to true for everything except POST.</summary>
        public bool? Idempotent { get; set; }

        /// <summary>Sends ConsistencyLevel: eventual, which Graph needs for advanced queries such as $count.</summary>
        public bool EventualConsistency { get; set; }

        /// <summary>Message returned to the caller on 404 instead of the generic one.</summary>
        public string NotFoundMessage { get; set; }
    }
}
