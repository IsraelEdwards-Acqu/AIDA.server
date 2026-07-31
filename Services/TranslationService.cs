using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIDA.Server.Services
{
    public class TranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _subscriptionKey;
        private readonly string? _region;
        private readonly ILogger<TranslationService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public TranslationService(HttpClient httpClient, IConfiguration configuration, ILogger<TranslationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _subscriptionKey = configuration["TRANSLATOR_KEY"] ?? configuration["Translator:Key"] ?? string.Empty;
            _region = configuration["TRANSLATOR_REGION"] ?? configuration["Translator:Region"];

            if (string.IsNullOrWhiteSpace(_subscriptionKey))
            {
                _logger.LogWarning("TranslationService initialized without a subscription key. Translation calls will fail until configured.");
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Translate text to the specified target language using Azure Cognitive Services Translator.
        /// Returns the translated text on success; on failure returns the original input text.
        /// </summary>
        public async Task<string> TranslateAsync(string text, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            if (string.IsNullOrWhiteSpace(targetLang)) targetLang = "en";

            if (string.IsNullOrWhiteSpace(_subscriptionKey))
            {
                _logger.LogError("Translation attempted but TRANSLATOR_KEY is not configured.");
                return text;
            }

            try
            {
                var endpoint = $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to={Uri.EscapeDataString(targetLang)}";

                var requestBody = new[] { new { Text = text } };
                var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                // Required headers for Azure Translator
                request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
                if (!string.IsNullOrWhiteSpace(_region))
                {
                    request.Headers.Add("Ocp-Apim-Subscription-Region", _region);
                }

                // Optional: accept JSON
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Translator API returned {StatusCode}. Response: {Response}", response.StatusCode, responseContent);
                    return text;
                }

                var results = JsonSerializer.Deserialize<List<TranslatorResponse>>(responseContent, _jsonOptions);
                if (results == null || results.Count == 0 || results[0].Translations == null || results[0].Translations.Count == 0)
                {
                    _logger.LogWarning("Translator API returned an unexpected payload. Raw response: {Response}", responseContent);
                    return text;
                }

                return results[0].Translations[0].Text ?? text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Translator API for targetLang={TargetLang}", targetLang);
                return text;
            }
        }

        // DTOs for deserialization of Azure Translator response
        private class TranslatorResponse
        {
            public List<TranslationItem>? Translations { get; set; }
        }

        private class TranslationItem
        {
            public string? Text { get; set; }
            public string? To { get; set; }
        }
    }
}
