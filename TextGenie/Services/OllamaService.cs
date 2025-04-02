using System.Net.Http;
using System.Text;
using System.Text.Json;
using TextGenie.Models;
using System.Text.Json.Serialization;

namespace TextGenie.Services
{
    public class OllamaService : ITextProcessingService
    {
        private readonly HttpClient _httpClient;
        private const string BASE_URL = "http://localhost:11434/api/generate";
        private readonly string _defaultModel = "mistral"; // Default model, can be changed

        public OllamaService(ApiConfig config)
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> RewriteTextAsync(string text, string style = "professional")
        {
            var prompt = $"Rewrite the following text in a {style} style. Only return the rewritten text without any additional commentary:\n\n{text}";
            return await SendRequestAsync(prompt);
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage = "English")
        {
            var prompt = $"Translate the following text to {targetLanguage}. Only return the translated text without any additional commentary:\n\n{text}";
            return await SendRequestAsync(prompt);
        }

        public async Task<string> SummarizeTextAsync(string text, int maxLength = 100)
        {
            var prompt = $"Summarize the following text in {maxLength} words or less. Only return the summary without any additional commentary:\n\n{text}";
            return await SendRequestAsync(prompt);
        }

        private async Task<string> SendRequestAsync(string prompt)
        {
            try
            {
                var payload = new
                {
                    model = _defaultModel,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.7,
                        top_p = 0.9
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                System.Diagnostics.Debug.WriteLine($"Sending request to Ollama:");
                System.Diagnostics.Debug.WriteLine($"Request Body: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(BASE_URL, content);
                var responseString = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Raw Response Body: {responseString}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"API request failed with status {response.StatusCode}: {responseString}");
                }

                var responseObject = JsonSerializer.Deserialize<OllamaResponse>(responseString);
                System.Diagnostics.Debug.WriteLine($"Deserialized Response: {responseObject?.Response}");

                if (responseObject?.Response == null)
                {
                    throw new Exception("No response received from Ollama");
                }

                // Clean up the response by removing any potential markdown formatting
                var cleanResponse = responseObject.Response
                    .Replace("```", "")
                    .Trim();

                System.Diagnostics.Debug.WriteLine($"Final Clean Response: {cleanResponse}");
                return cleanResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in SendRequestAsync: {ex}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private class OllamaResponse
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("created_at")]
            public string CreatedAt { get; set; } = string.Empty;

            [JsonPropertyName("response")]
            public string Response { get; set; } = string.Empty;

            [JsonPropertyName("done")]
            public bool Done { get; set; }

            [JsonPropertyName("done_reason")]
            public string DoneReason { get; set; } = string.Empty;

            [JsonPropertyName("context")]
            public int[] Context { get; set; } = Array.Empty<int>();

            [JsonPropertyName("total_duration")]
            public long TotalDuration { get; set; }

            [JsonPropertyName("load_duration")]
            public long LoadDuration { get; set; }

            [JsonPropertyName("prompt_eval_count")]
            public int PromptEvalCount { get; set; }

            [JsonPropertyName("prompt_eval_duration")]
            public long PromptEvalDuration { get; set; }

            [JsonPropertyName("eval_count")]
            public int EvalCount { get; set; }

            [JsonPropertyName("eval_duration")]
            public long EvalDuration { get; set; }
        }
    }
} 