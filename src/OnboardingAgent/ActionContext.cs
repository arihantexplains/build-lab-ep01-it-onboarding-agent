using Microsoft.Xrm.Sdk;

namespace OnboardingAgent
{
    /// <summary>
    /// Everything a function needs for one call.
    /// </summary>
    public sealed class ActionContext
    {
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

        public Config Config { get; }

        public GraphClient Graph { get; }

        /// <summary>Organization service running as SYSTEM.</summary>
        public IOrganizationService Service { get; }

        public ITracingService Trace { get; }

        public Deadline Deadline { get; }

        public DirectoryService Directory { get; }

        public Validator Validator { get; }

        public string RequestedBy { get; }

        public string ApprovalId { get; }

        public string CorrelationId { get; }

        public StepLog NewStepLog()
        {
            return new StepLog(Trace);
        }
    }
}
