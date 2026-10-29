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

        public StepLog(ITracingService trace)
        {
            _trace = trace;
            Steps = new JsonArray();
        }

        public JsonArray Steps { get; }

        public bool HasFailures => _failures.Count > 0;

        public string FailureSummary => string.Join(" ", _failures);

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

        public void Done(string name, string detail)
        {
            Add(name, "done", detail);
        }

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
