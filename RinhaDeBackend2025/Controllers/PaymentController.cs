using Microsoft.AspNetCore.Mvc;
using RinhaDeBackend2025.Models;
using RinhaDeBackend2025.Services;
using RinhaDeBackend2025.Services.Interfaces;

namespace RinhaDeBackend2025.Controllers;

public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentSummaryService _paymentSummaryService;

    public PaymentController(IPaymentService paymentService, IPaymentSummaryService paymentSummaryService)
    {
        _paymentService = paymentService;
        _paymentSummaryService = paymentSummaryService;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Route("/payments")]
    public async Task<IActionResult> Payment([FromBody] PaymentModel model)
    {
        var result = await _paymentService.ProcessPaymentAsync(model);

        if (result)
        {
            return Ok(new { message = "payment processed successfully"});
        }
            
        return StatusCode(502, new { message = "processor failed" });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Route("/payments-summary")]
    public async Task<IActionResult> PaymentSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var summary = await _paymentSummaryService.GetSummaryAsync(from, to);
        
        var summaries = summary
            .GroupBy(e => e.Processor)
            .ToDictionary(f => f.Key, g => new
            {
                totalRequests = g.Count(),
                totalAmount = g.Sum(x => x.Amount)
            });
        
        return Ok(summaries);
    }
}