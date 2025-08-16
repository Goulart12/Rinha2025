using RinhaDeBackend2025.Models;
using RinhaDeBackend2025.Services.Interfaces;

namespace RinhaDeBackend2025.Services;

public class QueuedHostedService : BackgroundService
{
    private readonly ILogger<QueuedHostedService> _logger;
    private readonly IBackgroundTaskQueue<PaymentModel> _taskQueue;
    private readonly IServiceProvider _serviceProvider;

    public QueuedHostedService(
        ILogger<QueuedHostedService> logger, 
        IBackgroundTaskQueue<PaymentModel> taskQueue, 
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queued Hosted Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var paymentModel = await _taskQueue.DequeueAsync(stoppingToken);
                
                using (var scope = _serviceProvider.CreateScope())
                {
                    var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                    await paymentService.ProcessPaymentAsync(paymentModel);
                }
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing task.");
            }
        }

        _logger.LogInformation("Queued Hosted Service is stopping.");
    }
}
