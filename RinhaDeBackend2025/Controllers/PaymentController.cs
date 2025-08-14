using Microsoft.AspNetCore.Mvc;
using RinhaDeBackend2025.Models;
using RinhaDeBackend2025.Services.Interfaces;

namespace RinhaDeBackend2025.Controllers;

public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentSummaryService _paymentSummaryService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, IPaymentSummaryService paymentSummaryService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _paymentSummaryService = paymentSummaryService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Route("payments")]
    public async Task<IActionResult> Payment([FromBody] PaymentModel model)
    {
        _logger.LogInformation("PaymentController: Received payment request for CorrelationId: {CorrelationId}", model.CorrelationId);
        var result = await _paymentService.ProcessPaymentAsync(model);

        if (result)
        {
            return Ok(new { message = "payment processed successfully" });
        }

        return StatusCode(502, new { message = "processor failed" });
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