using System.Text;
using System.Text.Json;
using RinhaDeBackend2025.Models;
using RinhaDeBackend2025.Services.Interfaces;

namespace RinhaDeBackend2025.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentSummaryService _summaryService;
    private readonly IHealthCheckService _healthyHealthCheckService;
    private readonly ILogger<PaymentService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _defaultProcessor;
    private readonly string _fallbackProcessor;
    private const int MaxAttempts = 5;

    public PaymentService(string defaultProcessor, string fallbackProcessor, IPaymentSummaryService summaryService, IHealthCheckService healthyHealthCheckService, ILogger<PaymentService> logger, IHttpClientFactory httpClientFactory)
    {
        _defaultProcessor = defaultProcessor;
        _fallbackProcessor = fallbackProcessor;
        _summaryService = summaryService;
        _healthyHealthCheckService = healthyHealthCheckService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<bool> ProcessPaymentAsync(PaymentModel paymentModel)
    {
        var paymentData = new
        {
            correlationId = paymentModel.CorrelationId,
            amount = paymentModel.Amount,
            requestedAt = DateTime.Now,
        };
        
        var processorCheck = await _healthyHealthCheckService.IsHealthy(_defaultProcessor);
        var fallbackCheck = await _healthyHealthCheckService.IsHealthy(_fallbackProcessor);

        // if (processorCheck)
        // {
        //     for (var i = 0; i < MaxAttempts; i++)
        //     {
        //         var response = await DefaultProcessPaymentAsync(paymentData);
        //
        //         if (!response)
        //         {
        //             var fallbackResponse = await FallbackProcessPaymentAsync(paymentData);
        //
        //             if (!fallbackResponse)
        //             {
        //                 _logger.LogError($"Falling attempt {i + 1} of {MaxAttempts}");
        //             }
        //             
        //             await _summaryService.InsertSummaryAsync("fallback", paymentData.amount);
        //             return true;
        //         }
        //
        //         await _summaryService.InsertSummaryAsync("default", paymentData.amount);
        //         return true;
        //     }
        //     
        //     _logger.LogError($"Failed processing payment {paymentData}");
        //     return false;
        // }
        
        if (fallbackCheck)
        {
            var fallbackResponse = await FallbackProcessPaymentAsync(paymentData);

            if (fallbackResponse)
            {
                await _summaryService.InsertSummaryAsync("fallback", paymentData.amount);
                return true;
            }
        }
        
        return false;
    }

    private async Task<bool> DefaultProcessPaymentAsync(object paymentData)
    {
        using var httpClient = _httpClientFactory.CreateClient();

        var json = JsonSerializer.Serialize(paymentData, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"{_defaultProcessor}/payments", content);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        
        return false;
    }

    private async Task<bool> FallbackProcessPaymentAsync(object paymentData)
    {
        using var httpClient = _httpClientFactory.CreateClient();

        var json = JsonSerializer.Serialize(paymentData, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"{_fallbackProcessor}/payments", content);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        
        return false;
    }
}