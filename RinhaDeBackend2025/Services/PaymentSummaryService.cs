using System.Text.Json;
using RinhaDeBackend2025.Models;
using RinhaDeBackend2025.Services.Interfaces;
using StackExchange.Redis;

namespace RinhaDeBackend2025.Services;

public class PaymentSummaryService : IPaymentSummaryService
{
    private readonly IDatabase _redisDatabase;
    private const String Key = "payment:summary";

    public PaymentSummaryService(IConnectionMultiplexer redisDatabase)
    {
        _redisDatabase = redisDatabase.GetDatabase();
    }

    public async Task<List<PaymentSummaryEntry>> GetSummaryAsync(DateTime from, DateTime to)
    {
        var start = new DateTimeOffset(from).ToUnixTimeSeconds();
        var end = new DateTimeOffset(to).ToUnixTimeSeconds();
        
        var entries = await _redisDatabase.SortedSetRangeByScoreAsync(Key, start, end);
        var summaryEntries = new List<PaymentSummaryEntry>();

        foreach (var entry in entries)
        {
            var summaryEntry = JsonSerializer.Deserialize<PaymentSummaryEntry>(entry);
            if (summaryEntry != null) 
            {
                summaryEntries.Add(summaryEntry);
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
        var timestamp = new DateTimeOffset(entry.TimeStamp).ToUnixTimeSeconds();
        
        await _redisDatabase.SortedSetAddAsync(Key, json, timestamp);
    }
}