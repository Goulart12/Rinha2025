using System.Text.Json;
using RinhaDeBackend2025.Models;
using RinhaDeBackend2025.Services.Interfaces;
using StackExchange.Redis;

namespace RinhaDeBackend2025.Services;

public class PaymentSummaryService : IPaymentSummaryService
{
    private readonly IDatabase _redisDatabase;
    private readonly ILogger<PaymentSummaryService> _logger;
    private const String Key = "payment:summary";

    public PaymentSummaryService(IConnectionMultiplexer redisDatabase, ILogger<PaymentSummaryService> logger)
    {
        _redisDatabase = redisDatabase.GetDatabase();
        _logger = logger;
    }

    public async Task<List<PaymentSummaryEntry>> GetSummaryAsync(DateTime from, DateTime to)
    {
        _logger.LogInformation("GetSummaryAsync called with from: {from} and to: {to}", from, to);
        var start = new DateTimeOffset(from).ToUnixTimeSeconds();
        var end = new DateTimeOffset(to).ToUnixTimeSeconds();
        
        var entries = await _redisDatabase.SortedSetRangeByScoreAsync(Key, start, end);
        _logger.LogInformation("Redis returned {count} entries.", entries.Length);
        var summaryEntries = new List<PaymentSummaryEntry>();

        foreach (var entry in entries)
        {
            try
            {
                var summaryEntry = JsonSerializer.Deserialize<PaymentSummaryEntry>(entry);
                if (summaryEntry != null) 
                {
                    summaryEntries.Add(summaryEntry);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize Redis entry. Value: {entry}", (string)entry);
                throw new Exception("Failed to deserialize Redis entry. Value: {entry}", ex);
            }
        }
        
        return summaryEntries;
    }

    public async Task InsertSummaryAsync(string processor, decimal amount)
    {
        var entry = new PaymentSummaryEntry
        {
            Processor = processor,
            Amount = amount,
            TimeStamp = DateTime.UtcNow
        };
        
        var json = JsonSerializer.Serialize(entry);
        _logger.LogInformation("InsertSummaryAsync called with: {json}", json);
        var timestamp = new DateTimeOffset(entry.TimeStamp).ToUnixTimeSeconds();
        
        try
        {
            await _redisDatabase.SortedSetAddAsync(Key, json, timestamp);
        }
        catch (JsonException ex)
        {
           _logger.LogError(ex, "Failed to insert Redis entry. Value: {json}", json);
           throw new Exception("Failed to insert Redis entry. Value: {json}", ex);
        }
    }
}
