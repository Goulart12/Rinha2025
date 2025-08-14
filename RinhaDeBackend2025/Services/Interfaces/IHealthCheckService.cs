namespace RinhaDeBackend2025.Services.Interfaces;

public interface IHealthCheckService
{
    Task<bool> IsHealthy(string clientName);
}