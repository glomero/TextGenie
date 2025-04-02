using System.IO;
using Microsoft.Extensions.Configuration;
using TextGenie.Models;

namespace TextGenie.Services
{
    public class ConfigurationService
    {
        private readonly IConfiguration _configuration;

        public ConfigurationService()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
        }

        public ApiConfig GetApiConfig(string provider)
        {
            var section = _configuration.GetSection($"ApiSettings:{provider}");
            var config = new ApiConfig
            {
                ApiKey = section["ApiKey"] ?? string.Empty,
                Endpoint = section["Endpoint"] ?? string.Empty,
                Model = section["Model"] ?? string.Empty
            };

            // Log configuration (without exposing the full API key)
            var maskedKey = string.IsNullOrEmpty(config.ApiKey) ? "Not Set" : 
                           config.ApiKey.Length > 8 ? $"{config.ApiKey.Substring(0, 4)}...{config.ApiKey.Substring(config.ApiKey.Length - 4)}" : 
                           "Invalid Key";
            
            System.Diagnostics.Debug.WriteLine($"Loading configuration for {provider}:");
            System.Diagnostics.Debug.WriteLine($"API Key: {maskedKey}");
            System.Diagnostics.Debug.WriteLine($"Endpoint: {config.Endpoint}");
            System.Diagnostics.Debug.WriteLine($"Model: {config.Model}");

            return config;
        }

        public void UpdateApiKey(string provider, string apiKey)
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = File.ReadAllText(configPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);

            config.ApiSettings[provider].ApiKey = apiKey;
            File.WriteAllText(configPath, Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));
        }
    }
} 