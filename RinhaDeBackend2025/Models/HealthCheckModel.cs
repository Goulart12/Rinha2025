namespace RinhaDeBackend2025.Models;

public class HealthCheckModel
{
    public bool Failing { get; set; }
    public int MinResponseTime { get; set; }
}