using System.Net.Http;
using System.Text;
using System.Text.Json;
using TextGenie.Models;

namespace TextGenie.Services
{
    public class HuggingFaceService : ITextProcessingService
    {
        private readonly ApiConfig _config;
        private readonly HttpClient _httpClient;
        private const string BASE_URL = "https://api-inference.huggingface.co/models/";
        private readonly string _defaultModel = "facebook/bart-large-cnn"; // Good for summarization
        private readonly string _translationModel = "facebook/mbart-large-50-many-to-many-mmt"; // Good for translation
        private readonly string _rewriteModel = "facebook/bart-large-xsum"; // Good for rewriting

        public HuggingFaceService(ApiConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
        }

        public async Task<string> RewriteTextAsync(string text, string style = "professional")
        {
            try
            {
                var payload = new { inputs = $"Rewrite this in a {style} style: {text}" };
                return await SendRequestAsync(payload, _rewriteModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RewriteTextAsync: {ex}");
                throw;
            }
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage = "English")
        {
            try
            {
                var payload = new { inputs = $"Translate to {targetLanguage}: {text}" };
                return await SendRequestAsync(payload, _translationModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in TranslateTextAsync: {ex}");
                throw;
            }
        }

        public async Task<string> SummarizeTextAsync(string text, int maxLength = 100)
        {
            try
            {
                var payload = new { 
                    inputs = text,
                    parameters = new { 
                        max_length = maxLength,
                        min_length = maxLength / 2,
                        do_sample = false
                    }
                };
                return await SendRequestAsync(payload, _defaultModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SummarizeTextAsync: {ex}");
                throw;
            }
        }

        private async Task<string> SendRequestAsync(object payload, string model)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                System.Diagnostics.Debug.WriteLine($"Sending request to HuggingFace model {model}:");
                System.Diagnostics.Debug.WriteLine($"Request Body: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{BASE_URL}{model}";
                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response Body: {responseString}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"API request failed with status {response.StatusCode}: {responseString}");
                }

                // Parse the response based on the model type
                if (model == _defaultModel || model == _rewriteModel)
                {
                    var responseArray = JsonSerializer.Deserialize<List<HuggingFaceResponse>>(responseString);
                    return responseArray?[0]?.Summary ?? responseArray?[0]?.GeneratedText ?? "No response from model";
                }
                else
                {
                    var responseArray = JsonSerializer.Deserialize<List<TranslationResponse>>(responseString);
                    return responseArray?[0]?.TranslationText ?? "No response from model";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in SendRequestAsync: {ex}");
                throw;
            }
        }

        private class HuggingFaceResponse
        {
            public string? Summary { get; set; }
            public string? GeneratedText { get; set; }
        }

        private class TranslationResponse
        {
            public string TranslationText { get; set; } = string.Empty;
        }
    }
} 