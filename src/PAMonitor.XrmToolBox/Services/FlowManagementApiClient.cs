using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PAMonitor.XrmToolBox.Models;

namespace PAMonitor.XrmToolBox.Services
{
    /// <summary>
    /// Calls Power Automate (ProcessSimple) REST API for per-action run details.
    /// API is unofficial; used at own risk (same as many community tools).
    /// </summary>
    public sealed class FlowManagementApiClient
    {
        private const string ApiVersion = "2016-11-01";
        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private readonly FlowTokenProvider _tokenProvider;
        private readonly string _environmentId;

        public FlowManagementApiClient(FlowTokenProvider tokenProvider, string environmentId)
        {
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            if (string.IsNullOrWhiteSpace(environmentId))
            {
                throw new ArgumentException("Environment id is required.", nameof(environmentId));
            }

            _environmentId = environmentId.Trim();
        }

        public async Task<IReadOnlyList<FlowActionInfo>> GetRunActionsAsync(
            string flowId,
            string runName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(flowId))
            {
                throw new ArgumentException("Flow id is required.", nameof(flowId));
            }

            if (string.IsNullOrWhiteSpace(runName))
            {
                throw new ArgumentException("Run name is required.", nameof(runName));
            }

            var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            var url =
                $"https://api.flow.microsoft.com/providers/Microsoft.ProcessSimple/environments/{Uri.EscapeDataString(_environmentId)}" +
                $"/flows/{Uri.EscapeDataString(flowId)}" +
                $"/runs/{Uri.EscapeDataString(runName)}" +
                $"/actions?api-version={ApiVersion}";

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(
                            $"Flow API {(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(body, 500)}");
                    }

                    return ParseActions(body);
                }
            }
        }

        public static string BuildDetailedErrorSummary(IEnumerable<FlowActionInfo> actions)
        {
            var failed = actions?
                .Where(a => string.Equals(a.Status, "Failed", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(a.Status, "Faulted", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (failed == null || failed.Count == 0)
            {
                return null;
            }

            return string.Join(Environment.NewLine + Environment.NewLine,
                failed.Select(a =>
                    $"Action: {a.Name}{Environment.NewLine}" +
                    $"Status: {a.Status}{Environment.NewLine}" +
                    $"Code:   {a.Code}{Environment.NewLine}" +
                    $"Error:  {a.ErrorMessage}"));
        }

        private static IReadOnlyList<FlowActionInfo> ParseActions(string json)
        {
            var root = JToken.Parse(json);
            var items = root["value"] as JArray ?? new JArray(root);
            var list = new List<FlowActionInfo>();

            foreach (var item in items.OfType<JObject>())
            {
                var props = item["properties"] as JObject ?? item;
                var error = props["error"] as JObject
                            ?? props["outputs"]?["body"]?["error"] as JObject
                            ?? props["outputs"]?["error"] as JObject;

                string message = null;
                if (error != null)
                {
                    message = error["message"]?.ToString();
                }
                else if (props["error"] != null && props["error"].Type == JTokenType.String)
                {
                    message = props["error"].ToString();
                }
                else if (props["error"] != null)
                {
                    message = props["error"].ToString();
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    message = props["code"]?.ToString();
                }

                list.Add(new FlowActionInfo
                {
                    Name = item["name"]?.ToString() ?? props["name"]?.ToString(),
                    Status = props["status"]?.ToString(),
                    Code = error?["code"]?.ToString() ?? props["code"]?.ToString(),
                    ErrorMessage = message,
                    StartTime = ParseDate(props["startTime"]),
                    EndTime = ParseDate(props["endTime"])
                });
            }

            return list;
        }

        private static DateTime? ParseDate(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (DateTime.TryParse(token.ToString(), out var dt))
            {
                return dt;
            }

            return null;
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, max) + "…";
        }
    }
}
