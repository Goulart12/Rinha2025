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
        _logger.LogInformation("Starting payment process for CorrelationId: {CorrelationId}", paymentModel.CorrelationId);
        var paymentData = new
        {
            correlationId = paymentModel.CorrelationId,
            amount = paymentModel.Amount,
            requestedAt = DateTime.Now,
        };
        
        var processorCheck = await _healthyHealthCheckService.IsHealthy("default");
        _logger.LogInformation("Default processor health check result: {Status}", processorCheck);

        if (processorCheck)
        {
            _logger.LogInformation("Attempting payment with default processor.");
            var response = await DefaultProcessPaymentAsync(paymentData);
    
            if (!response)
            {
                _logger.LogWarning("Default processor failed. Attempting fallback processor.");
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
        
        _logger.LogWarning("Default processor is not healthy. Checking fallback processor.");
        var fallbackCheck = await _healthyHealthCheckService.IsHealthy("fallback");
        _logger.LogInformation("Fallback processor health check result: {Status}", fallbackCheck);

        if (fallbackCheck)
        {
            _logger.LogInformation("Attempting payment with fallback processor.");
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

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully processed payment with default processor.");
            return true;
        }
        
        _logger.LogWarning("Failed to process payment with default processor. Status code: {StatusCode}", response.StatusCode);
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
            _logger.LogInformation("Successfully processed payment with fallback processor.");
            return true;
        }
        
        _logger.LogWarning("Failed to process payment with fallback processor. Status code: {StatusCode}", response.StatusCode);
        return false;
    }
}
