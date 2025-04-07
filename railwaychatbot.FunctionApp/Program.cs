using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry;
using railwaychatbot.AIEngine.Impl;
using railwaychatbot.AIEngine;
using Microsoft.Extensions.DependencyInjection;
using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Microsoft.SemanticKernel;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();


builder.Services.AddKernel().AddAzureOpenAIChatCompletion(builder.Configuration["AZURE_OPENAI_CHAT_DEPLOYMENT_NAME"]!, builder.Configuration["AZURE_OPENAI_ENDPOINT"]!, builder.Configuration["AZURE_OPENAI_API_KEY"]!);
builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Warning));
builder.Services.AddSingleton(serviceProvider => new CosmosClient(builder.Configuration["COSMOS_DB_CONNECTION_STRING"]));
builder.Services.AddSingleton(serviceProvider => new AzureOpenAIClient(new Uri(builder.Configuration["AZURE_OPENAI_ENDPOINT"]!), new Azure.AzureKeyCredential(builder.Configuration["AZURE_OPENAI_API_KEY"]!)));
builder.Services.AddScoped<IAIEngine, AIEngine>();
builder.Services.AddScoped<IMotoreOrarioAIAgent, MotoreOrarioAIAgent>();
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();

bool enableOT;
if (bool.TryParse(builder.Configuration["ENABLE_OPENTELEMETRY_TRACING"]!, out enableOT) && enableOT)
{
    var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService("railwaychatbot.AIEngine.Impl");

    // Enable model diagnostics with sensitive data.
    AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

    var traceProvider = Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(resourceBuilder)
        .AddSource("Microsoft.SemanticKernel*")
        .AddConsoleExporter()
        .Build();

    var meterProvider = Sdk.CreateMeterProviderBuilder().SetResourceBuilder(resourceBuilder)
        .AddMeter("Microsoft.SemanticKernel*")
        .AddConsoleExporter()
        .Build();

    var loggerFactory = LoggerFactory.Create(builder =>
    {
        // Add OpenTelemetry as a logging provider
        builder.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resourceBuilder);
            // Format log messages. This is default to false.
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });
        builder.SetMinimumLevel(LogLevel.Information);
    });

    builder.Services.AddSingleton(loggerFactory);
}

builder.Build().Run();
