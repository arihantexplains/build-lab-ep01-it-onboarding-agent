using System;

namespace OnboardingAgent
{
    /// <summary>
    /// A failed Graph call. The exception message is the friendly text; the raw Graph details are kept separately.
    /// </summary>
    public sealed class GraphException : AdminActionException
    {
        private const int MaxGraphMessageLength = 300;

        private GraphException(string friendlyMessage, int statusCode, string code, string graphMessage, string requestId)
            : base(friendlyMessage)
        {
            StatusCode = statusCode;
            Code = code;
            GraphMessage = graphMessage;
            RequestId = requestId;
        }

        /// <summary>HTTP status code returned by Graph.</summary>
        public int StatusCode { get; }

        /// <summary>Graph error code, such as Request_ResourceNotFound, or null.</summary>
        public string Code { get; }

        /// <summary>Raw Graph error message, cut to 300 characters, or null.</summary>
        public string GraphMessage { get; }

        /// <summary>Graph request ID for support cases, or null.</summary>
        public string RequestId { get; }

        /// <summary>True when an add-member call failed because the user is already a member.</summary>
        public bool IsAlreadyMember => StatusCode == 400 && Mentions(GraphMessage, "added object references already exist");

        /// <summary>True when the change clashes with existing data, such as a duplicate user principal name.</summary>
        public bool IsConflict => StatusCode == 409 || (StatusCode == 400 && Mentions(GraphMessage, "conflicting object"));

        /// <summary>True when the error looks like a newly created object has not reached every directory replica yet.</summary>
        public bool IsReplicationDelay => StatusCode == 404
            || (StatusCode == 400 && (string.Equals(Code, "Request_ResourceNotFound", StringComparison.OrdinalIgnoreCase)
                || Mentions(GraphMessage, "does not exist or one of its queried reference-property objects are not present")));

        /// <summary>Builds the exception from a failed Graph response, pulling out the error code, message and request ID.</summary>
        /// <param name="statusCode">HTTP status code.</param>
        /// <param name="body">Response body. Non-JSON bodies are ignored.</param>
        /// <param name="notFoundMessage">Message to use for 404, or null for the generic one.</param>
        /// <returns>An exception with a friendly message for the caller.</returns>
        public static GraphException FromResponse(int statusCode, string body, string notFoundMessage)
        {
            string code = null;
            string message = null;
            string requestId = null;
            try
            {
                var error = Json.ParseObject(body).GetObject("error");
                if (error != null)
                {
                    code = error.GetString("code");
                    message = error.GetString("message");
                    requestId = error.GetObject("innerError")?.GetString("request-id");
                }
            }
            catch (FormatException)
            {
                // Non-JSON error bodies (gateway pages) carry nothing useful.
            }

            if (message != null && message.Length > MaxGraphMessageLength)
            {
                message = message.Substring(0, MaxGraphMessageLength) + "...";
            }

            return new GraphException(Friendly(statusCode, message, notFoundMessage), statusCode, code, message, requestId);
        }

        private static string Friendly(int statusCode, string graphMessage, string notFoundMessage)
        {
            switch (statusCode)
            {
                case 400:
                    return "Microsoft Graph rejected the request: " + (graphMessage ?? "bad request.");
                case 401:
                    return "Microsoft Graph did not accept the app's credentials. Check the tenant ID, client ID and client secret environment variables.";
                case 403:
                    return "Access denied by Microsoft Graph. The app registration is missing a permission or directory role this action needs.";
                case 404:
                    return notFoundMessage ?? "The user, group or resource was not found in Microsoft Entra ID.";
                case 409:
                    return "The change conflicts with the current state in Microsoft Entra ID: " + (graphMessage ?? "conflict.");
                case 429:
                    return "Microsoft Graph is throttling requests. Wait a few minutes and try again.";
                default:
                    return statusCode >= 500
                        ? "Microsoft Graph is temporarily unavailable (HTTP " + statusCode + "). Try again shortly."
                        : "Microsoft Graph returned HTTP " + statusCode + ": " + (graphMessage ?? "no details.");
            }
        }

        private static bool Mentions(string text, string fragment)
        {
            return text != null && text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
