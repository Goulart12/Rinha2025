using Microsoft.AspNetCore.Mvc;
using RinhaDeBackend2025.Models;
using RinhaDeBackend2025.Services.Interfaces;

namespace RinhaDeBackend2025.Controllers;

public class PaymentController : ControllerBase
{
    private readonly IBackgroundTaskQueue<PaymentModel> _taskQueue;
    private readonly IPaymentSummaryService _paymentSummaryService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IBackgroundTaskQueue<PaymentModel> taskQueue,
        IPaymentSummaryService paymentSummaryService,
        ILogger<PaymentController> logger)
    {
        _taskQueue = taskQueue;
        _paymentSummaryService = paymentSummaryService;
        _logger = logger;
    }

    [HttpPost]
    [Route("payments")]
    public IActionResult Payment([FromBody] PaymentModel model)
    {
        _logger.LogInformation("Enqueuing payment request for CorrelationId: {CorrelationId}", model.CorrelationId);
        _taskQueue.Enqueue(model);
        // return Accepted(new { message = "payment received and queued for processing" });
        return Ok(new { message =  "payment processed successfully" });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Route("payments-summary")]
    public async Task<IActionResult> PaymentSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var summary = await _paymentSummaryService.GetSummaryAsync(from, to);

        var result = new Dictionary<string, object>
        {
            ["default"] = new { totalRequests = 0, totalAmount = 0.0m },
            ["fallback"] = new { totalRequests = 0, totalAmount = 0.0m }
        };

        var groupedByProcessor = summary.GroupBy(e => e.Processor);

        foreach (var group in groupedByProcessor)
        {
            result[group.Key] = new
            {
                totalRequests = group.Count(),
                totalAmount = group.Sum(x => x.Amount)
            };
        }

        return Ok(result);
    }
}
