using RinhaDeBackend2025.Models;

namespace RinhaDeBackend2025.Services.Interfaces;

public interface IPaymentSummaryService
{
    Task<List<PaymentSummaryEntry>> GetSummaryAsync(DateTime from, DateTime to);
    Task InsertSummaryAsync(string processor, decimal amount);
}