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
        if (await _summaryService.AlreadyExists(paymentModel.CorrelationId)) return false;
        
        _logger.LogInformation("Starting payment process for CorrelationId: {CorrelationId}", paymentModel.CorrelationId);
        var paymentData = new
        {
            correlationId = paymentModel.CorrelationId,
            amount = paymentModel.Amount,
            requestedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        };
        
        var processorCheck = await _healthyHealthCheckService.IsHealthy("default");
        _logger.LogInformation("Default processor health check result: {Status}", processorCheck);

        if (processorCheck)
        {
            var response = await DefaultProcessPaymentAsync(paymentData);
    
            if (!response)
            {
                var fallbackResponse = await FallbackProcessPaymentAsync(paymentData);
    
                if (!fallbackResponse)
                {
                    _logger.LogError("Fallback processor also failed for CorrelationId: {CorrelationId}", paymentModel.CorrelationId);
                }
                
                await _summaryService.InsertSummaryAsync("fallback", paymentData.amount);
                _logger.LogInformation("Payment processed and recorded using fallback processor for CorrelationId: {CorrelationId}", paymentModel.CorrelationId);
                return true;
            }
    
            await _summaryService.InsertSummaryAsync("default", paymentData.amount);
            _logger.LogInformation("Payment processed and recorded using default processor for CorrelationId: {CorrelationId}", paymentModel.CorrelationId);
            return true;
        }
        
        var fallbackCheck = await _healthyHealthCheckService.IsHealthy("fallback");
        _logger.LogInformation("Fallback processor health check result: {Status}", fallbackCheck);

        if (fallbackCheck)
        {
            var fallbackResponse = await FallbackProcessPaymentAsync(paymentData);

            if (fallbackResponse)
            {
                await _summaryService.InsertSummaryAsync("fallback", paymentData.amount);
                _logger.LogInformation("Payment processed and recorded using fallback processor for CorrelationId: {CorrelationId}", paymentModel.CorrelationId);
                return true;
            }
        }
        
        _logger.LogError("All processors are unavailable or failed to process payment for CorrelationId: {CorrelationId}", paymentModel.CorrelationId);
        return false;
    }

    private async Task<bool> DefaultProcessPaymentAsync(object paymentData)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        var json = JsonSerializer.Serialize(paymentData, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"{_defaultProcessor}/payments", content);

        return response.IsSuccessStatusCode;
    }

    private async Task<bool> FallbackProcessPaymentAsync(object paymentData)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        var json = JsonSerializer.Serialize(paymentData, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"{_fallbackProcessor}/payments", content);

        return response.IsSuccessStatusCode;
    }
}
