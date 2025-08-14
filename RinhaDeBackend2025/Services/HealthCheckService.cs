using System.Collections.Concurrent;
using System.Text.Json;
using RinhaDeBackend2025.Models;
using RinhaDeBackend2025.Services.Interfaces;

namespace RinhaDeBackend2025.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDictionary<string, string> _processorUrls;
    
    private readonly ConcurrentDictionary<string, (DateTime, bool)> _healthStatusCache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    public HealthCheckService(IHttpClientFactory httpClientFactory, IDictionary<string, string> processorUrls)
    {
        _httpClientFactory = httpClientFactory;
        _processorUrls = processorUrls;
    }

    public async Task<bool> IsHealthy(string clientName)
    {
        var now = DateTime.UtcNow;

        if (_healthStatusCache.TryGetValue(clientName, out var status) && (now - status.Item1).TotalSeconds < 5)
        {
            return status.Item2;
        }

        var semaphore = _semaphores.GetOrAdd(clientName, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();
        try
        {
            if (_healthStatusCache.TryGetValue(clientName, out status) && (now - status.Item1).TotalSeconds < 5)
            {
                return status.Item2;
            }

            var isHealthy = await PerformHealthCheck(clientName);
            _healthStatusCache[clientName] = (now, isHealthy);
            return isHealthy;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<bool> PerformHealthCheck(string clientName)
    {
        if (!_processorUrls.TryGetValue(clientName, out var baseUrl))
        {
            return false;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3); 
            var response = await client.GetAsync($"{baseUrl}/payments/service-health");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var healthResponse = JsonSerializer.Deserialize<HealthCheckModel>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return healthResponse is { Failing: false };
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}