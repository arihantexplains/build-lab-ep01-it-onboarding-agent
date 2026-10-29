using Microsoft.Xrm.Sdk;

namespace OnboardingAgent
{
    /// <summary>
    /// Everything a function needs for one call.
    /// </summary>
    public sealed class ActionContext
    {
        /// <summary>Builds the context and creates the directory and validator helpers that share the same Graph client.</summary>
        /// <param name="config">Settings loaded from environment variables.</param>
        /// <param name="graph">Client for Microsoft Graph calls.</param>
        /// <param name="service">Organization service running as SYSTEM.</param>
        /// <param name="trace">Plug-in trace log.</param>
        /// <param name="deadline">Time budget for this call.</param>
        /// <param name="requestedBy">Who asked for the action, recorded in the audit log.</param>
        /// <param name="approvalId">Approval reference for high-risk actions, or null.</param>
        /// <param name="correlationId">ID that links this call to its audit row and trace entries.</param>
        public ActionContext(Config config, GraphClient graph, IOrganizationService service, ITracingService trace, Deadline deadline,
            string requestedBy, string approvalId, string correlationId)
        {
            Config = config;
            Graph = graph;
            Service = service;
            Trace = trace;
            Deadline = deadline;
            RequestedBy = requestedBy;
            ApprovalId = approvalId;
            CorrelationId = correlationId;
            Directory = new DirectoryService(graph, config);
            Validator = new Validator(config, graph, Directory, trace);
        }

        /// <summary>Settings loaded from environment variables.</summary>
        public Config Config { get; }

        /// <summary>Client for Microsoft Graph calls.</summary>
        public GraphClient Graph { get; }

        /// <summary>Organization service running as SYSTEM.</summary>
        public IOrganizationService Service { get; }

        /// <summary>Plug-in trace log for diagnostics.</summary>
        public ITracingService Trace { get; }

        /// <summary>Time budget for this call.</summary>
        public Deadline Deadline { get; }

        /// <summary>Shared lookups for users, groups and roles.</summary>
        public DirectoryService Directory { get; }

        /// <summary>Guardrail checks to run before any change.</summary>
        public Validator Validator { get; }

        /// <summary>Who asked for the action, recorded in the audit log.</summary>
        public string RequestedBy { get; }

        /// <summary>Approval reference for high-risk actions, or null when none was given.</summary>
        public string ApprovalId { get; }

        /// <summary>ID that links this call to its audit row and trace entries.</summary>
        public string CorrelationId { get; }

        /// <summary>Starts a new step log that writes unexpected errors to this call's trace.</summary>
        public StepLog NewStepLog()
        {
            return new StepLog(Trace);
        }
    }
}
