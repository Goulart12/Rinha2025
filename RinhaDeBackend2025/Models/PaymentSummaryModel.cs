namespace RinhaDeBackend2025.Models;

public class PaymentSummaryModel
{
    public int TotalRequests {get; set;}
    public decimal TotalAmount {get; set;}
    public decimal TotalFee { get; set; }
    public decimal FeePerTransaction { get; set; }
}