using RinhaDeBackend2025.Models;

namespace RinhaDeBackend2025.Services.Interfaces;

public interface IPaymentService
{
    public Task<bool> ProcessPaymentAsync(PaymentModel paymentModel);
}