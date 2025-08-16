using Microsoft.OpenApi.Models;
using RinhaDeBackend2025.Services;
using RinhaDeBackend2025.Services.Interfaces;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

var defaultProcessor = Environment.GetEnvironmentVariable("PROCESSOR_DEFAULT_URL") ?? "http://payment-processor-default:8080";
var fallbackProcessor = Environment.GetEnvironmentVariable("PROCESSOR_FALLBACK_URL") ?? "http://payment-processor-fallback:8080";
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "localhost::6379";

builder.WebHost.UseUrls("http://0.0.0.0:8080");
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
        
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

// builder.Services.AddHttpClient("default", client => client.BaseAddress = new Uri("http://payment-processor-default:8080"));                
// builder.Services.AddHttpClient("fallback", client => client.BaseAddress = new Uri("http://payment-processor-fallback:8080"));
builder.Services.AddHttpClient();

builder.Services.AddSingleton(typeof(IBackgroundTaskQueue<>), typeof(BackgroundTaskQueue<>));
builder.Services.AddHostedService<QueuedHostedService>();

builder.Services.AddSingleton<IHealthCheckService>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var processorUrls = new Dictionary<string, string>
    {
        { "default", defaultProcessor },
        { "fallback", fallbackProcessor }
    };
    return new HealthCheckService(httpClientFactory, processorUrls);
});
builder.Services.AddScoped<IPaymentSummaryService>(provider =>
{
    var redis = provider.GetRequiredService<IConnectionMultiplexer>();
    var logger = provider.GetRequiredService<ILogger<PaymentSummaryService>>();
    return new PaymentSummaryService(redis, logger);
});

var redisConfig = ConfigurationOptions.Parse(redisConnectionString);
redisConfig.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));

builder.Services.AddScoped<IPaymentService>(provider =>
{
    var summaryService = provider.GetRequiredService<IPaymentSummaryService>();
    var healthCheckService = provider.GetRequiredService<IHealthCheckService>();
    var logger = provider.GetRequiredService<ILogger<PaymentService>>();
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    return new PaymentService(defaultProcessor, fallbackProcessor, summaryService, healthCheckService, logger, httpClientFactory);
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.MapGet("/", async (IHealthCheckService healthCheck) =>
{
    var defaultProccessorStatus = await healthCheck.IsHealthy("default");
    var fallbackProccessorStatus = await healthCheck.IsHealthy("fallback");
    return new
    {
        message = "o pai ta on",
        database = new
        {
            defaultProccessorStatus,
            fallbackProccessorStatus
        }
    };
});

app.Run();