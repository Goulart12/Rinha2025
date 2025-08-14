namespace RinhaDeBackend2025.Models;

public class PaymentModel
{
    public Guid CorrelationId { get; set; }
    public decimal Amount { get; set; }
}