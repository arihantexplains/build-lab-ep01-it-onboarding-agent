using System;
using Microsoft.Xrm.Sdk;

namespace OnboardingAgent
{
    /// <summary>
    /// Plug-in behind the ab_ExecuteAdminAction Custom API. Never throws: every outcome, including errors,
    /// is returned through the output parameters so the audit row is not rolled back with the transaction.
    /// </summary>
    public sealed class ExecuteAdminAction : IPlugin
    {
        // Leaves headroom inside the 2-minute sandbox limit for loading config and writing the audit row.
        private static readonly TimeSpan TimeBudget = TimeSpan.FromSeconds(90);

        public void Execute(IServiceProvider serviceProvider)
        {
            var deadline = new Deadline(TimeBudget);
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            // SYSTEM, so callers need no privileges on environment variables or the audit table.
            var service = factory.CreateOrganizationService(null);

            var actionName = Input(context, "ActionName")?.Trim();
            var parametersJson = Input(context, "ParametersJson");
            var requestedBy = Input(context, "RequestedBy")?.Trim();
            var approvalId = Input(context, "ApprovalId")?.Trim();
            if (string.IsNullOrEmpty(approvalId))
            {
                approvalId = null;
            }

            var correlationId = Guid.NewGuid().ToString("D");
            trace.Trace("Action {0}, correlation {1}, platform correlation {2}.", actionName, correlationId, context.CorrelationId);

            ActionResult result;
            try
            {
                result = Run(context, service, trace, deadline, actionName, parametersJson, requestedBy, approvalId, correlationId);
            }
            catch (AdminActionException ex)
            {
                result = ActionResult.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                trace.Trace("Unexpected error: {0}", ex);
                result = ActionResult.Fail("An unexpected error occurred. Quote correlation ID " + correlationId + " when reporting it.");
            }

            var resultJson = Json.Serialize(result.Data);
            var message = result.Message;
            var audited = new AuditLogger(service, trace).TryWrite(actionName, requestedBy, approvalId, correlationId, parametersJson,
                result.Success, message, resultJson, deadline.Elapsed);
            if (!audited)
            {
                message += " Warning: the audit log entry could not be written.";
            }

            trace.Trace("Finished {0}: success={1} in {2} ms.", actionName, result.Success, (long)deadline.Elapsed.TotalMilliseconds);
            context.OutputParameters["Success"] = result.Success;
            context.OutputParameters["Message"] = message;
            context.OutputParameters["ResultJson"] = resultJson;
            context.OutputParameters["CorrelationId"] = correlationId;
        }

        private static ActionResult Run(IPluginExecutionContext context, IOrganizationService service, ITracingService trace, Deadline deadline,
            string actionName, string parametersJson, string requestedBy, string approvalId, string correlationId)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                throw new ValidationException("ActionName is required.");
            }

            if (string.IsNullOrEmpty(requestedBy))
            {
                throw new ValidationException("RequestedBy is required.");
            }

            Func<ActionContext, JsonObject, ActionResult> handler;
            if (!ActionRouter.TryGet(actionName, out handler))
            {
                throw new ValidationException("Unknown action '" + actionName + "'. Supported actions: " + string.Join(", ", ActionRouter.ActionNames) + ".");
            }

            if (ActionRouter.RequiresApproval(actionName))
            {
                Validator.RequireApproval(approvalId, actionName);
            }

            JsonObject parameters;
            try
            {
                parameters = Json.ParseObject(parametersJson);
            }
            catch (FormatException ex)
            {
                throw new ValidationException("ParametersJson is not a valid JSON object: " + ex.Message);
            }

            var config = Config.Get(service, context.OrganizationId);
            var graph = new GraphClient(config, trace, deadline, correlationId);
            var actionContext = new ActionContext(config, graph, service, trace, deadline, requestedBy, approvalId, correlationId);
            return handler(actionContext, parameters);
        }

        private static string Input(IPluginExecutionContext context, string name)
        {
            return context.InputParameters.Contains(name) ? context.InputParameters[name] as string : null;
        }
    }
}
