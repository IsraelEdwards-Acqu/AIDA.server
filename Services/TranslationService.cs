using AIDA.Server.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class TranslationService
{
    private readonly HttpClient _httpClient;

    public TranslationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> TranslateAsync(string text, string targetLang)
    {
        var requestBody = $"[{{\"Text\":\"{text}\"}}]";

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to={targetLang}");

        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        // Add your subscription key and region headers here
        request.Headers.Add("Ocp-Apim-Subscription-Key", "YOUR_KEY_HERE");
        request.Headers.Add("Ocp-Apim-Subscription-Region", "YOUR_REGION_HERE");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();

        var translations = JsonSerializer.Deserialize<List<TranslationResult>>(result);
        return translations[0].Translations[0].Text;
    }
}
