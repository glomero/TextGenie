using TextGenie.Models;

namespace TextGenie.Services
{
    public class TextProcessingServiceFactory
    {
        private readonly Dictionary<string, ITextProcessingService> _services;
        private readonly ConfigurationService _configService;

        public TextProcessingServiceFactory()
        {
            _services = new Dictionary<string, ITextProcessingService>();
            _configService = new ConfigurationService();
        }

        public ITextProcessingService GetService(string provider)
        {
            if (_services.TryGetValue(provider, out var service))
            {
                return service;
            }

            var config = _configService.GetApiConfig(provider);
            
            // Ollama doesn't need an API key since it runs locally
            if (provider.ToLower() != "ollama" && string.IsNullOrEmpty(config.ApiKey))
            {
                throw new InvalidOperationException($"API key not configured for {provider}. Please configure it in appsettings.json");
            }

            service = provider.ToLower() switch
            {
                "openai" => new OpenAiService(config),
                "claude" => new ClaudeService(config),
                "chatgpt" => new ChatGPTService(config),
                "huggingface" => new HuggingFaceService(config),
                "ollama" => new OllamaService(config),
                _ => throw new ArgumentException($"Unsupported provider: {provider}")
            };

            _services[provider] = service;
            return service;
        }
    }
} 