using System.Linq;
using System.Net;
using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Sends from the configured mailbox to internal recipients only, with a footer naming the requester.
    /// </summary>
    public static class SendEmail
    {
        /// <summary>Checks every recipient is internal, adds the footer and sends the message.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: to, cc, subject, body and isHtml.</param>
        /// <returns>The ActionResult sent back to the agent, with the recipients, subject and sending mailbox.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = SendEmailParameters.Parse(parameters);
            foreach (var address in p.To.Concat(p.Cc))
            {
                ctx.Validator.RequireInternalRecipient(address);
            }

            var footer = p.IsHtml
                ? "<p style=\"color:#666;font-size:12px\">Sent by IT admin automation for " + WebUtility.HtmlEncode(ctx.RequestedBy)
                    + ". Reference " + ctx.CorrelationId + ".</p>"
                : "\r\n\r\n--\r\nSent by IT admin automation for " + ctx.RequestedBy + ". Reference " + ctx.CorrelationId + ".";

            ctx.Directory.SendMail(p.To, p.Cc, p.Subject, p.Body + footer, p.IsHtml, true);

            return ActionResult.Ok("Email sent to " + (p.To.Count + p.Cc.Count) + " recipient(s).", new JsonObject
            {
                { "to", new JsonArray(p.To) },
                { "cc", new JsonArray(p.Cc) },
                { "subject", p.Subject },
                { "from", ctx.Config.SenderMailbox }
            });
        }
    }
}
