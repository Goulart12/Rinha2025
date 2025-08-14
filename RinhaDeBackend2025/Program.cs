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

builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.AddScoped<IPaymentSummaryService, PaymentSummaryService>();

var redisConfig = ConfigurationOptions.Parse(redisConnectionString);
redisConfig.AbortOnConnectFail = false;
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));

builder.Services.AddSingleton<IPaymentService>(provider =>
{
    var summaryService = provider.GetRequiredService<IPaymentSummaryService>();
    var healthCheckService = provider.GetRequiredService<IHealthCheckService>();
    var logger = provider.GetRequiredService<ILogger<PaymentService>>();
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    return new PaymentService(defaultProcessor, fallbackProcessor, summaryService, healthCheckService, logger, httpClientFactory);
});

var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();