using System;
using System.Diagnostics;

namespace OnboardingAgent
{
    /// <summary>
    /// Time budget for one plug-in call, so retries and paging never push past the sandbox limit.
    /// </summary>
    public sealed class Deadline
    {
        private static readonly TimeSpan MaxRequestTimeout = TimeSpan.FromSeconds(25);
        private static readonly TimeSpan MinimumForRequest = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan PagingReserve = TimeSpan.FromSeconds(20);

        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly TimeSpan _budget;

        public Deadline(TimeSpan budget)
        {
            _budget = budget;
        }

        public TimeSpan Elapsed => _clock.Elapsed;

        public TimeSpan Remaining => _budget - _clock.Elapsed;

        public TimeSpan RequestTimeout => Remaining < MaxRequestTimeout ? Remaining : MaxRequestTimeout;

        /// <summary>True when there is too little time left to fetch another page and still finish the action.</summary>
        public bool ShouldStopPaging => Remaining < PagingReserve;

        public bool CanWait(TimeSpan wait)
        {
            return Remaining - wait > MinimumForRequest;
        }

        public void ThrowIfExpired()
        {
            if (Remaining < MinimumForRequest)
            {
                throw new AdminActionException("The action stopped because it reached its time limit. Check the result for the steps that completed.");
            }
        }
    }
}
