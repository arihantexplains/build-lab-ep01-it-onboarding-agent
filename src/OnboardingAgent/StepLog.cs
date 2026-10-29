using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace OnboardingAgent
{
    /// <summary>
    /// Outcome of each step in a multi-step action, so partial progress is reported rather than hidden.
    /// </summary>
    public sealed class StepLog
    {
        private readonly ITracingService _trace;
        private readonly List<string> _failures = new List<string>();

        /// <summary>Creates an empty step log.</summary>
        /// <param name="trace">Trace log that receives full details of unexpected errors.</param>
        public StepLog(ITracingService trace)
        {
            _trace = trace;
            Steps = new JsonArray();
        }

        /// <summary>Recorded steps, each with step name, status and detail.</summary>
        public JsonArray Steps { get; }

        /// <summary>True when any step failed.</summary>
        public bool HasFailures => _failures.Count > 0;

        /// <summary>All failure messages joined into one line, each prefixed with its step name.</summary>
        public string FailureSummary => string.Join(" ", _failures);

        /// <summary>Runs one step and records it as done or failed, so a failure does not stop the remaining steps.</summary>
        /// <param name="name">Step name shown in the result.</param>
        /// <param name="action">The work to do. Returns the detail text for a successful step.</param>
        /// <returns>True when the step succeeded.</returns>
        public bool Run(string name, Func<string> action)
        {
            try
            {
                Done(name, action());
                return true;
            }
            catch (AdminActionException ex)
            {
                Failed(name, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _trace.Trace("Step '{0}' failed unexpectedly: {1}", name, ex);
                Failed(name, "Unexpected error; see the plug-in trace log.");
                return false;
            }
        }

        /// <summary>Records a step as done.</summary>
        /// <param name="name">Step name shown in the result.</param>
        /// <param name="detail">What the step did.</param>
        public void Done(string name, string detail)
        {
            Add(name, "done", detail);
        }

        /// <summary>Records a step as skipped, for example when there was nothing to change.</summary>
        /// <param name="name">Step name shown in the result.</param>
        /// <param name="reason">Why the step was skipped.</param>
        public void Skip(string name, string reason)
        {
            Add(name, "skipped", reason);
        }

        private void Failed(string name, string message)
        {
            Add(name, "failed", message);
            _failures.Add(name + ": " + message);
        }

        private void Add(string name, string status, string detail)
        {
            Steps.Add(new JsonObject { { "step", name }, { "status", status }, { "detail", detail } });
        }
    }
}
