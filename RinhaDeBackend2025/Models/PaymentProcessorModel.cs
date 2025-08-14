namespace RinhaDeBackend2025.Models;

public class PaymentProcessorModel
{
    public Guid CorrelationId { get; set; }
    public decimal Amount { get; set; }
    public DateTime RequestedAt { get; set; } 
}