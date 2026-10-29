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

        /// <summary>Starts the clock for one plug-in call.</summary>
        /// <param name="budget">Total time the call may use before it must stop.</param>
        public Deadline(TimeSpan budget)
        {
            _budget = budget;
        }

        /// <summary>Time used since the call started.</summary>
        public TimeSpan Elapsed => _clock.Elapsed;

        /// <summary>Time left in the budget. Can be negative once the budget is spent.</summary>
        public TimeSpan Remaining => _budget - _clock.Elapsed;

        /// <summary>Timeout for the next HTTP request: the time left, capped at 25 seconds.</summary>
        public TimeSpan RequestTimeout => Remaining < MaxRequestTimeout ? Remaining : MaxRequestTimeout;

        /// <summary>True when there is too little time left to fetch another page and still finish the action.</summary>
        public bool ShouldStopPaging => Remaining < PagingReserve;

        /// <summary>Checks whether a retry delay still leaves enough time for one more request.</summary>
        /// <param name="wait">The delay being considered.</param>
        /// <returns>True when more than 5 seconds would remain after waiting.</returns>
        public bool CanWait(TimeSpan wait)
        {
            return Remaining - wait > MinimumForRequest;
        }

        /// <summary>Stops the action with a clear message when less than 5 seconds remain.</summary>
        /// <exception cref="AdminActionException">The time budget is nearly spent.</exception>
        public void ThrowIfExpired()
        {
            if (Remaining < MinimumForRequest)
            {
                throw new AdminActionException("The action stopped because it reached its time limit. Check the result for the steps that completed.");
            }
        }
    }
}
