using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Microsoft.Xrm.Sdk;

namespace OnboardingAgent
{
    /// <summary>
    /// Microsoft Graph calls with an app-only token. Request bodies are never traced because some carry passwords.
    /// </summary>
    public sealed class GraphClient
    {
        public const string GraphRoot = "https://graph.microsoft.com/v1.0";

        private const int MaxTries = 3;
        private static readonly HttpMethod PatchMethod = new HttpMethod("PATCH");
        private static readonly HttpClient Http = CreateHttpClient();
        private static readonly object TokenLock = new object();
        private static readonly Dictionary<string, AccessToken> Tokens = new Dictionary<string, AccessToken>(StringComparer.OrdinalIgnoreCase);

        private readonly Config _config;
        private readonly ITracingService _trace;
        private readonly Deadline _deadline;
        private readonly string _correlationId;

        public GraphClient(Config config, ITracingService trace, Deadline deadline, string correlationId)
        {
            _config = config;
            _trace = trace;
            _deadline = deadline;
            _correlationId = correlationId;
        }

        private string TokenKey => _config.TenantId + "|" + _config.ClientId;

        public JsonObject Get(string path, GraphCallOptions options = null)
        {
            return Send(HttpMethod.Get, path, null, options);
        }

        public JsonObject Post(string path, object body, GraphCallOptions options = null)
        {
            return Send(HttpMethod.Post, path, JsonBody(body), options);
        }

        public JsonObject Patch(string path, object body, GraphCallOptions options = null)
        {
            return Send(PatchMethod, path, JsonBody(body), options);
        }

        public JsonObject Put(string path, object body, GraphCallOptions options = null)
        {
            return Send(HttpMethod.Put, path, JsonBody(body), options);
        }

        public void Delete(string path, GraphCallOptions options = null)
        {
            Send(HttpMethod.Delete, path, null, options);
        }

        public JsonObject PutBytes(string path, byte[] content, string contentType, GraphCallOptions options = null)
        {
            return Send(HttpMethod.Put, path, () =>
            {
                var body = new ByteArrayContent(content);
                body.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                return body;
            }, options);
        }

        /// <summary>
        /// Follows @odata.nextLink until maxItems is reached or time runs short; truncated says whether anything was left unread.
        /// </summary>
        public List<JsonObject> GetAll(string path, int maxItems, out bool truncated, GraphCallOptions options = null)
        {
            var items = new List<JsonObject>();
            truncated = false;
            var next = path;
            while (next != null)
            {
                var page = Get(next, options);
                foreach (var item in page.GetArray("value").Objects())
                {
                    if (items.Count >= maxItems)
                    {
                        truncated = true;
                        return items;
                    }

                    items.Add(item);
                }

                next = page.GetString("@odata.nextLink");
                if (next != null && _deadline.ShouldStopPaging)
                {
                    truncated = true;
                    break;
                }
            }

            return items;
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            // Sandbox workers are recycled and idle sockets get dropped, so reusing keep-alive connections fails intermittently.
            client.DefaultRequestHeaders.ConnectionClose = true;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // Some Graph endpoints (PIM) fail with a culture error unless a concrete language is sent.
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
            return client;
        }

        private static Func<HttpContent> JsonBody(object body)
        {
            if (body == null)
            {
                return null;
            }

            var json = Json.Serialize(body);
            // HttpClient disposes request content after sending, so each retry needs a fresh instance.
            return () => new StringContent(json, Encoding.UTF8, "application/json");
        }

        private JsonObject Send(HttpMethod method, string path, Func<HttpContent> content, GraphCallOptions options)
        {
            options = options ?? new GraphCallOptions();
            var url = ToUrl(path);
            var idempotent = options.Idempotent ?? method != HttpMethod.Post;
            var tokenRefreshed = false;

            for (var attempt = 1; ; attempt++)
            {
                _deadline.ThrowIfExpired();
                var token = GetToken();
                var timer = Stopwatch.StartNew();

                using (var request = new HttpRequestMessage(method, url))
                using (var cancel = new CancellationTokenSource(_deadline.RequestTimeout))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    request.Headers.TryAddWithoutValidation("client-request-id", _correlationId);
                    if (options.EventualConsistency)
                    {
                        request.Headers.TryAddWithoutValidation("ConsistencyLevel", "eventual");
                    }

                    if (content != null)
                    {
                        request.Content = content();
                    }

                    HttpResponseMessage response;
                    try
                    {
                        response = Http.SendAsync(request, cancel.Token).ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException)
                    {
                        _trace.Trace("Graph {0} {1} timed out after {2} ms.", method.Method, TracePath(url), timer.ElapsedMilliseconds);
                        throw new AdminActionException("Microsoft Graph did not respond in time. The change may or may not have been applied; check before trying again.");
                    }
                    catch (HttpRequestException ex)
                    {
                        _trace.Trace("Graph {0} {1} network error: {2}", method.Method, TracePath(url), ex.GetBaseException().Message);
                        var wait = Backoff(attempt);
                        if (idempotent && attempt < MaxTries && _deadline.CanWait(wait))
                        {
                            Thread.Sleep(wait);
                            continue;
                        }

                        throw new AdminActionException("Could not reach Microsoft Graph. Try again shortly.", ex);
                    }

                    using (response)
                    {
                        var body = response.Content == null
                            ? string.Empty
                            : response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        var status = (int)response.StatusCode;
                        _trace.Trace("Graph {0} {1} -> {2} in {3} ms (try {4}).", method.Method, TracePath(url), status, timer.ElapsedMilliseconds, attempt);

                        if (response.IsSuccessStatusCode)
                        {
                            return ParseSuccess(response, body);
                        }

                        var error = GraphException.FromResponse(status, body, options.NotFoundMessage);
                        _trace.Trace("Graph error {0} {1}: {2} (request-id {3}).", status, error.Code, error.GraphMessage, error.RequestId);

                        if (status == 401 && !tokenRefreshed)
                        {
                            // A cached token can be revoked early; fetch a new one without spending a retry.
                            InvalidateToken();
                            tokenRefreshed = true;
                            attempt--;
                            continue;
                        }

                        var delay = RetryDelay(response, error, attempt, idempotent, options);
                        if (delay.HasValue && attempt < MaxTries && _deadline.CanWait(delay.Value))
                        {
                            _trace.Trace("Retrying in {0:0.#} s.", delay.Value.TotalSeconds);
                            Thread.Sleep(delay.Value);
                            continue;
                        }

                        throw error;
                    }
                }
            }
        }

        private static TimeSpan? RetryDelay(HttpResponseMessage response, GraphException error, int attempt, bool idempotent, GraphCallOptions options)
        {
            var status = error.StatusCode;
            if (status == 429 || (idempotent && (status == 502 || status == 503 || status == 504)))
            {
                return RetryAfter(response) ?? Backoff(attempt);
            }

            if (options.RetryOnReplicationDelay && error.IsReplicationDelay)
            {
                return TimeSpan.FromSeconds(2 * attempt);
            }

            return null;
        }

        private static TimeSpan? RetryAfter(HttpResponseMessage response)
        {
            var header = response.Headers.RetryAfter;
            if (header == null)
            {
                return null;
            }

            if (header.Delta.HasValue)
            {
                return header.Delta.Value;
            }

            if (header.Date.HasValue)
            {
                var wait = header.Date.Value - DateTimeOffset.UtcNow;
                return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
            }

            return null;
        }

        private static TimeSpan Backoff(int attempt)
        {
            return TimeSpan.FromSeconds(Math.Pow(2, attempt));
        }

        private static JsonObject ParseSuccess(HttpResponseMessage response, string body)
        {
            var mediaType = response.Content?.Headers.ContentType?.MediaType ?? string.Empty;
            if (string.IsNullOrWhiteSpace(body) || mediaType.IndexOf("json", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return new JsonObject();
            }

            try
            {
                return Json.ParseObject(body);
            }
            catch (FormatException)
            {
                throw new AdminActionException("Microsoft Graph returned a response that could not be read.");
            }
        }

        private static string ToUrl(string path)
        {
            if (path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Only nextLinks arrive as absolute URLs; never send the token anywhere else.
                if (!path.StartsWith(GraphRoot + "/", StringComparison.OrdinalIgnoreCase))
                {
                    throw new AdminActionException("Refusing to follow a link outside Microsoft Graph.");
                }

                return path;
            }

            return GraphRoot + (path.StartsWith("/", StringComparison.Ordinal) ? path : "/" + path);
        }

        private static string TracePath(string url)
        {
            var path = url.StartsWith(GraphRoot, StringComparison.OrdinalIgnoreCase) ? url.Substring(GraphRoot.Length) : url;
            var query = path.IndexOf('?');
            return query < 0 ? path : path.Substring(0, query);
        }

        private string GetToken()
        {
            lock (TokenLock)
            {
                AccessToken cached;
                if (Tokens.TryGetValue(TokenKey, out cached) && cached.ExpiresUtc > DateTime.UtcNow.AddMinutes(2))
                {
                    return cached.Value;
                }

                var fresh = RequestToken();
                Tokens[TokenKey] = fresh;
                return fresh.Value;
            }
        }

        private void InvalidateToken()
        {
            lock (TokenLock)
            {
                Tokens.Remove(TokenKey);
            }
        }

        private AccessToken RequestToken()
        {
            _deadline.ThrowIfExpired();
            var url = "https://login.microsoftonline.com/" + Uri.EscapeDataString(_config.TenantId) + "/oauth2/v2.0/token";
            var form = new Dictionary<string, string>
            {
                { "client_id", _config.ClientId },
                { "client_secret", _config.ClientSecret },
                { "scope", "https://graph.microsoft.com/.default" },
                { "grant_type", "client_credentials" }
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(form) })
            using (var cancel = new CancellationTokenSource(_deadline.RequestTimeout))
            {
                HttpResponseMessage response;
                try
                {
                    response = Http.SendAsync(request, cancel.Token).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    throw new AdminActionException("Timed out while requesting a Microsoft Graph token.");
                }
                catch (HttpRequestException ex)
                {
                    throw new AdminActionException("Could not reach the Microsoft identity platform to get a token.", ex);
                }

                using (response)
                {
                    var body = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    JsonObject json;
                    try
                    {
                        json = Json.ParseObject(body);
                    }
                    catch (FormatException)
                    {
                        json = new JsonObject();
                    }

                    _trace.Trace("Token request -> {0}.", (int)response.StatusCode);
                    var token = json.GetString("access_token");
                    if (!response.IsSuccessStatusCode || token == null)
                    {
                        // The first line of error_description holds the AADSTS code and reason; later lines are trace IDs.
                        var description = json.GetString("error_description");
                        var firstLine = description?.Split('\r', '\n')[0];
                        throw new AdminActionException("Could not obtain a Microsoft Graph token: "
                            + (firstLine ?? json.GetString("error") ?? "HTTP " + (int)response.StatusCode));
                    }

                    return new AccessToken(token, DateTime.UtcNow.AddSeconds(json.GetInt("expires_in", 3599)));
                }
            }
        }
    }
}
