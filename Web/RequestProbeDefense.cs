#if NET48
using ByteForge.Toolkit.Configuration;
using ByteForge.Toolkit.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace ByteForge.Toolkit.Web
{
    /// <summary>
    /// Provides early-request probe blocking, throttling, and readable request diagnostics for logging.
    /// </summary>
    public static class RequestProbeDefense
    {
        private const string ClassificationItemKey = "ByteForge.RequestClassification";

        private static readonly string[] ForwardedHeaderNames =
        {
            "X-Forwarded-For",
            "X-Real-IP",
            "CF-Connecting-IP",
            "True-Client-IP",
            "Forwarded"
        };

        private static readonly ConcurrentDictionary<string, ProbeThrottleWindow> ProbeWindows =
            new ConcurrentDictionary<string, ProbeThrottleWindow>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Attempts to block a configured probe request before MVC routing executes.
        /// </summary>
        /// <param name="application">The current ASP.NET application.</param>
        /// <param name="isKnownEndpoint">Optional predicate that returns true for application-owned endpoints.</param>
        /// <returns><see langword="true"/> when the request was handled and should stop; otherwise <see langword="false"/>.</returns>
        public static bool TryHandleBlockedProbe(HttpApplication application, Func<HttpContextBase, bool> isKnownEndpoint = null)
        {
            if (application?.Context == null)
                return false;

            var context = new HttpContextWrapper(application.Context);
            var requestPath = NormalizePath(context.Request?.Url?.AbsolutePath ?? context.Request?.Path);
            if (string.IsNullOrWhiteSpace(requestPath))
                return false;

            if (isKnownEndpoint != null && isKnownEndpoint(context))
                return false;

            var settings = global::ByteForge.Toolkit.Configuration.Configuration.GetSection<ProbeDefenseSettings>("ProbeDefense") ?? new ProbeDefenseSettings();
            if (!settings.Enabled || !IsDeniedProbePath(requestPath, settings.DeniedPaths, settings.DeniedFragments))
                return false;

            var throttled = settings.ThrottlingEnabled && ShouldThrottle(context, settings);
            var classification = throttled ? "throttled-probe" : "blocked-probe";
            application.Context.Items[ClassificationItemKey] = classification;

            Log.Warning(BuildRequestLogMessage(
                context,
                404,
                throttled ? "Probe Request Throttled" : "Probe Request Blocked",
                classification));

            application.Response.Clear();
            application.Response.StatusCode = 404;
            application.Response.TrySkipIisCustomErrors = true;
            WriteBlockedResponse(application, context, 404, "Not Found");
            application.CompleteRequest();
            return true;
        }

        /// <summary>
        /// Determines the request classification for error logging.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="statusCode">The response status code.</param>
        /// <returns>A short classification label for the request.</returns>
        public static string GetClassification(HttpContextBase context, int statusCode)
        {
            if (context?.Items?[ClassificationItemKey] is string classification &&
                !string.IsNullOrWhiteSpace(classification))
            {
                return classification;
            }

            if (statusCode == 404)
            {
                var path = NormalizePath(context?.Request?.Url?.AbsolutePath ?? context?.Request?.Path);
                return HasFileExtension(path) ? "static-404" : "mvc-404";
            }

            return "request-error";
        }

        /// <summary>
        /// Builds a single-line, request-aware log message.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="statusCode">The response status code.</param>
        /// <param name="title">The friendly title for the log line.</param>
        /// <param name="classification">The request classification.</param>
        /// <returns>A compact log message suitable for file logging.</returns>
        public static string BuildRequestLogMessage(HttpContextBase context, int statusCode, string title, string classification)
        {
            var request = context?.Request;
            var builder = new StringBuilder();
            builder.Append($"[{statusCode}] {title}");
            builder.Append($" | class={Sanitize(classification)}");
            builder.Append($" | method={Sanitize(request?.HttpMethod)}");
            builder.Append($" | url={Sanitize(request?.RawUrl)}");
            builder.Append($" | host={Sanitize(request?.Url?.Authority ?? request?.Headers["Host"])}");
            builder.Append($" | ip={Sanitize(GetClientIp(context))}");
            builder.Append($" | forwarded={Sanitize(GetForwardedHeaders(context))}");
            builder.Append($" | referrer={Sanitize(request?.UrlReferrer?.ToString())}");
            builder.Append($" | ua={Sanitize(request?.UserAgent)}");
            return builder.ToString();
        }

        /// <summary>
        /// Determines if the request should be throttled based on the configured settings.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="settings">The probe defense settings.</param>
        /// <returns><see langword="true"/> if the request should be throttled; otherwise <see langword="false"/>.</returns>
        private static bool ShouldThrottle(HttpContextBase context, ProbeDefenseSettings settings)
        {
            var windowSeconds = Math.Max(1, settings.ThrottleWindowSeconds);
            var maxRequests = Math.Max(1, settings.ThrottleMaxRequestsPerWindow);
            var nowUtc = DateTime.UtcNow;
            var clientKey = BuildThrottleKey(context);
            var window = ProbeWindows.GetOrAdd(clientKey, _ => new ProbeThrottleWindow(nowUtc));

            lock (window.SyncRoot)
            {
                if ((nowUtc - window.WindowStartUtc).TotalSeconds >= windowSeconds)
                {
                    window.WindowStartUtc = nowUtc;
                    window.Count = 0;
                }

                window.Count++;
                return window.Count > maxRequests;
            }
        }

        /// <summary>
        /// Builds a key for throttling based on the request method and client IP.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>A string key for throttling.</returns>
        private static string BuildThrottleKey(HttpContextBase context)
        {
            var request = context?.Request;
            var method = request?.HttpMethod ?? "UNKNOWN";
            var clientIp = GetClientIp(context);
            return $"{method}|{clientIp}";
        }

        /// <summary>
        /// Determines whether a normalized request path matches configured probe path roots or probe fragments.
        /// </summary>
        /// <param name="requestPath">The normalized request path to inspect.</param>
        /// <param name="configuredPaths">The configured denied path roots.</param>
        /// <param name="configuredFragments">The configured denied path fragments.</param>
        /// <returns><see langword="true"/> when the request should be treated as a denied probe; otherwise <see langword="false"/>.</returns>
        private static bool IsDeniedProbePath(string requestPath, string[] configuredPaths, string[] configuredFragments)
        {
            if (string.IsNullOrWhiteSpace(requestPath))
                return false;

            if (configuredPaths?
                .Select(NormalizeConfiguredPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Any(path => requestPath.Equals(path, StringComparison.OrdinalIgnoreCase)
                    || requestPath.StartsWith(path + "/", StringComparison.OrdinalIgnoreCase)
                    || (path.EndsWith("/", StringComparison.Ordinal) && requestPath.StartsWith(path, StringComparison.OrdinalIgnoreCase))) == true)
            {
                return true;
            }

            return configuredFragments?
                .Select(fragment => fragment?.Trim())
                .Where(fragment => !string.IsNullOrWhiteSpace(fragment))
                .Any(fragment => requestPath.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0) == true;
        }

        /// <summary>
        /// Writes a probe-block response using the request's expected response format when it is clear.
        /// </summary>
        /// <param name="application">The current ASP.NET application.</param>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="statusCode">The response status code.</param>
        /// <param name="message">The response message.</param>
        private static void WriteBlockedResponse(HttpApplication application, HttpContextBase context, int statusCode, string message)
        {
            var format = ResolveExpectedResponseFormat(context);
            application.Response.ContentType = format.ContentType;
            application.Response.Write(format.FormatBody(statusCode, message));
        }

        /// <summary>
        /// Resolves the response format requested by the client.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>The response format to use.</returns>
        private static ProbeResponseFormat ResolveExpectedResponseFormat(HttpContextBase context)
        {
            var request = context?.Request;
            var accept = request?.Headers["Accept"] ?? string.Empty;
            var contentType = request?.ContentType ?? string.Empty;
            var path = NormalizePath(request?.Url?.AbsolutePath ?? request?.Path);
            var requestedWith = request?.Headers["X-Requested-With"] ?? string.Empty;

            if (ContainsMediaType(accept, "application/json")
                || ContainsMediaType(contentType, "application/json"))
            {
                return ProbeResponseFormat.Json;
            }

            if (ContainsMediaType(accept, "application/xml")
                || ContainsMediaType(accept, "text/xml")
                || ContainsMediaType(contentType, "application/xml")
                || ContainsMediaType(contentType, "text/xml"))
            {
                return ProbeResponseFormat.Xml;
            }

            if (string.Equals(requestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                return ProbeResponseFormat.Json;
            }

            return ProbeResponseFormat.Text;
        }

        /// <summary>
        /// Determines whether a header value contains a media type.
        /// </summary>
        /// <param name="value">The header value.</param>
        /// <param name="mediaType">The media type to find.</param>
        /// <returns><see langword="true"/> when the media type is present; otherwise <see langword="false"/>.</returns>
        private static bool ContainsMediaType(string value, string mediaType)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.IndexOf(mediaType, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Normalizes a configured path for comparison with request paths.
        /// </summary>
        /// <param name="path">The configured path to normalize.</param>
        /// <returns>The normalized path.</returns>
        private static string NormalizeConfiguredPath(string path)
        {
            path = NormalizePath(path);
            if (path.Length > 1 && path.EndsWith("/", StringComparison.Ordinal))
                return path.TrimEnd('/') + "/";

            return path;
        }

        /// <summary>
        /// Normalizes a request path by trimming and removing query strings or fragments.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var trimmed = path.Trim();
            var queryIndex = trimmed.IndexOfAny(new[] { '?', '#' });
            if (queryIndex >= 0)
                trimmed = trimmed.Substring(0, queryIndex);

            if (!trimmed.StartsWith("/", StringComparison.Ordinal))
                trimmed = "/" + trimmed;

            return trimmed;
        }

        /// <summary>
        /// Determines whether the path has a file extension.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns><see langword="true"/> if the path has a file extension; otherwise <see langword="false"/>.</returns>
        private static bool HasFileExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var extension = Path.GetExtension(path);
            return !string.IsNullOrWhiteSpace(extension);
        }

        /// <summary>
        /// Retrieves the client IP address from the request, checking forwarded headers.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>The client IP address.</returns>
        private static string GetClientIp(HttpContextBase context)
        {
            var request = context?.Request;
            var forwardedFor = request?.Headers["X-Forwarded-For"];
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var firstForwarded = forwardedFor
                    .Split(',')
                    .Select(part => part.Trim())
                    .FirstOrDefault(part => !string.IsNullOrWhiteSpace(part));

                if (!string.IsNullOrWhiteSpace(firstForwarded))
                    return firstForwarded;
            }

            return request?.UserHostAddress
                ?? request?.ServerVariables["REMOTE_ADDR"]
                ?? "unknown";
        }

        /// <summary>
        /// Retrieves the values of forwarded headers from the request.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>A string containing the forwarded header values, or "-" if none.</returns>
        private static string GetForwardedHeaders(HttpContextBase context)
        {
            var request = context?.Request;
            if (request == null)
                return "-";

            var values = ForwardedHeaderNames
                .Select(name => $"{name}={request.Headers[name]}")
                .Where(value => !value.EndsWith("=", StringComparison.Ordinal))
                .ToArray();

            return values.Length == 0 ? "-" : string.Join("; ", values);
        }

        /// <summary>
        /// Sanitizes a string value for logging by replacing control characters with spaces.
        /// </summary>
        /// <param name="value">The value to sanitize.</param>
        /// <returns>The sanitized string, or "-" if null or empty.</returns>
        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "-";

            var sanitized = value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Trim();

            return LogSecretMasker.Mask(sanitized);
        }

        /// <summary>
        /// Represents a response format used for blocked probe requests.
        /// </summary>
        private sealed class ProbeResponseFormat
        {
            /// <summary>
            /// The plain-text blocked-probe response format.
            /// </summary>
            public static readonly ProbeResponseFormat Text = new ProbeResponseFormat(
                "text/plain",
                (statusCode, message) => message);

            /// <summary>
            /// The JSON blocked-probe response format.
            /// </summary>
            public static readonly ProbeResponseFormat Json = new ProbeResponseFormat(
                "application/json",
                (statusCode, message) => JsonConvert.SerializeObject(new { success = false, statusCode, error = message }));

            /// <summary>
            /// The XML blocked-probe response format.
            /// </summary>
            public static readonly ProbeResponseFormat Xml = new ProbeResponseFormat(
                "application/xml",
                (statusCode, message) => $"<?xml version=\"1.0\" encoding=\"utf-8\"?><error><success>false</success><statusCode>{statusCode}</statusCode><message>{HttpUtility.HtmlEncode(message)}</message></error>");

            private readonly Func<int, string, string> formatBody;

            /// <summary>
            /// Initializes a new instance of the <see cref="ProbeResponseFormat"/> class.
            /// </summary>
            /// <param name="contentType">The response content type.</param>
            /// <param name="formatBody">The response body formatter.</param>
            private ProbeResponseFormat(string contentType, Func<int, string, string> formatBody)
            {
                ContentType = contentType;
                this.formatBody = formatBody;
            }

            /// <summary>
            /// Gets the response content type.
            /// </summary>
            public string ContentType { get; }

            /// <summary>
            /// Formats the response body.
            /// </summary>
            /// <param name="statusCode">The response status code.</param>
            /// <param name="message">The response message.</param>
            /// <returns>The formatted response body.</returns>
            public string FormatBody(int statusCode, string message)
            {
                return formatBody(statusCode, message);
            }
        }

        /// <summary>
        /// Represents a throttling window for tracking probe requests from a client.
        /// </summary>
        private sealed class ProbeThrottleWindow
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ProbeThrottleWindow"/> class.
            /// </summary>
            /// <param name="windowStartUtc">The start time of the window in UTC.</param>
            public ProbeThrottleWindow(DateTime windowStartUtc)
            {
                WindowStartUtc = windowStartUtc;
            }

            /// <summary>
            /// Gets the synchronization object for this throttle window.
            /// </summary>
            public object SyncRoot { get; } = new object();

            /// <summary>
            /// Gets or sets the start of the current throttle window in UTC.
            /// </summary>
            public DateTime WindowStartUtc { get; set; }

            /// <summary>
            /// Gets or sets the request count in the current throttle window.
            /// </summary>
            public int Count { get; set; }
        }
    }
}
#endif
