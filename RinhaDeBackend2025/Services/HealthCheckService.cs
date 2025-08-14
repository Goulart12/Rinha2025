using System.Text.Json;
using RinhaDeBackend2025.Models;
using RinhaDeBackend2025.Services.Interfaces;

namespace RinhaDeBackend2025.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDictionary<string, string> _processorUrls;
    private DateTime _lastTimeCheck = DateTime.MinValue;
    private bool? _healthy = null;

    public HealthCheckService(IHttpClientFactory httpClientFactory, IDictionary<string, string> processorUrls)
    {
        _httpClientFactory = httpClientFactory;
        _processorUrls = processorUrls;
    }


    public async Task<bool> IsHealthy(string clientName)
    {
        var now = DateTime.UtcNow;
        
        if ((now -  _lastTimeCheck).TotalSeconds >= 5)
        {
            var client = _httpClientFactory.CreateClient();
            if (_processorUrls.TryGetValue(clientName, out var baseUrl))
            {
                var response = await client.GetAsync($"{baseUrl}/payments/service-health");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var healthResponse = JsonSerializer.Deserialize<HealthCheckModel>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });

                    _healthy = healthResponse is { Failing: false };
                }
                else
                {
                    _healthy ??= true;
                }
            }
            else
            {
                _healthy = false;
            }
            
            _lastTimeCheck = now;
        }
        
        return _healthy ?? true;
    }
}
