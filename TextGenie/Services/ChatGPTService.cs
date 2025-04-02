using System.Net.Http;
using System.Text;
using System.Text.Json;
using TextGenie.Models;

namespace TextGenie.Services
{
    public class ChatGPTService : ITextProcessingService
    {
        private readonly ApiConfig _config;
        private readonly HttpClient _httpClient;

        public ChatGPTService(ApiConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
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
                    new { role = "system", content = "You are a helpful assistant that provides clear and concise responses." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 1000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_config.Endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<ChatGPTResponse>(responseString);

            return responseObject?.Choices?[0]?.Message?.Content ?? "Error: No response from ChatGPT";
        }

        private class ChatGPTResponse
        {
            public Choice[] Choices { get; set; } = Array.Empty<Choice>();
        }

        private class Choice
        {
            public Message Message { get; set; } = new();
        }

        private class Message
        {
            public string Content { get; set; } = string.Empty;
        }
    }
} 