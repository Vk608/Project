using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FABBatchValidator.Configuration;
using FABBatchValidator.Models;

namespace FABBatchValidator.Agent
{
    public class AgentApiException : Exception
    {
        public AgentApiException(string message, Exception? inner = null)
            : base(message, inner) { }
    }

    /// <summary>
    /// Thin HTTP client for calling FAB Agent.
    /// Sends query, returns raw response. No parsing logic here.
    /// </summary>
    public class AgentApiClient
    {
        private readonly AgentApiConfiguration _config;
        private readonly HttpClient _httpClient;

        public AgentApiClient(AgentApiConfiguration config, HttpClient? httpClient = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            ValidateConfig();

            _httpClient = httpClient ?? new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
            };
        }

        public async Task<AgentResponse> SendQueryAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            try
            {
                var payload = JsonSerializer.Serialize(new AgentRequest
                {
                    input = new InputData { query = query }
                });

                using var request = new HttpRequestMessage(HttpMethod.Post, _config.Url)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("x-user-id", _config.UserId);
                request.Headers.Add("x-authentication", $"api-key {_config.ApiKey}");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new AgentApiException(
                        $"Agent API {(int)response.StatusCode}: {body}");

                return JsonSerializer.Deserialize<AgentResponse>(body)
                    ?? throw new AgentApiException("Empty or invalid agent response");
            }
            catch (HttpRequestException ex)
            {
                throw new AgentApiException($"HTTP failure calling {_config.Url}", ex);
            }
            catch (JsonException ex)
            {
                throw new AgentApiException("Failed to parse agent response", ex);
            }
        }

        public AgentResponse SendQuery(string query)
            => SendQueryAsync(query).GetAwaiter().GetResult();

        private void ValidateConfig()
        {
            if (string.IsNullOrWhiteSpace(_config.Url))
                throw new AgentApiException("AgentApi.Url is required");

            if (string.IsNullOrWhiteSpace(_config.UserId))
                throw new AgentApiException("AgentApi.UserId is required");

            if (string.IsNullOrWhiteSpace(_config.ApiKey))
                throw new AgentApiException("AgentApi.ApiKey is required");

            if (_config.TimeoutSeconds <= 0)
                throw new AgentApiException("AgentApi.TimeoutSeconds must be > 0");
        }
    }

    public class AgentRequest
    {
        public InputData input { get; set; } = new();
    }

    public class InputData
    {
        public string query { get; set; } = string.Empty;
    }

    public class AgentResponse
    {
        public OutputData output { get; set; } = new();
    }

    public class OutputData
    {
        public string content { get; set; } = string.Empty;
        public System.Collections.Generic.List<UsedChunk> usedChunks { get; set; } = new();
    }
}
