using System.Net.Http;
using System.Text;
using System.Text.Json;
using TextGenie.Models;

namespace TextGenie.Services
{
    public class ClaudeService : ITextProcessingService
    {
        private readonly ApiConfig _config;
        private readonly HttpClient _httpClient;

        public ClaudeService(ApiConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        public async Task<string> RewriteTextAsync(string text, string style = "professional")
        {
            var prompt = $"Rewrite the following text in a {style} style: {text}";
            return await SendRequestAsync(prompt);
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage = "English")
        {
            var prompt = $"Translate the following text to {targetLanguage}: {text}";
            return await SendRequestAsync(prompt);
        }

        public async Task<string> SummarizeTextAsync(string text, int maxLength = 100)
        {
            var prompt = $"Summarize the following text in {maxLength} words or less: {text}";
            return await SendRequestAsync(prompt);
        }

        private async Task<string> SendRequestAsync(string prompt)
        {
            var requestBody = new
            {
                model = _config.Model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = prompt
                            }
                        }
                    }
                },
                max_tokens = 1000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_config.Endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<ClaudeResponse>(responseString);

            return responseObject?.Content?[0]?.Text?.Value ?? "Error: No response from Claude";
        }

        private class ClaudeResponse
        {
            public Content[] Content { get; set; } = Array.Empty<Content>();
        }

        private class Content
        {
            public Text Text { get; set; } = new();
        }

        private class Text
        {
            public string Value { get; set; } = string.Empty;
        }
    }
} 