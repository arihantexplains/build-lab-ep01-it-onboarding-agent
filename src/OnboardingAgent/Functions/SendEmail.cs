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
