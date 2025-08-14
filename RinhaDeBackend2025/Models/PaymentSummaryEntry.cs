namespace RinhaDeBackend2025.Models;

public class PaymentSummaryEntry
{
    public string Processor { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TimeStamp { get; set; }
}