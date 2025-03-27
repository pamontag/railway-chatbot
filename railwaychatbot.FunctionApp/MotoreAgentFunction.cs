using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using railwaychatbot.AIEngine;
using railwaychatbot.AIEngine.Impl;
using System.Net;
using System.Text.Json;
// https://stackoverflow.com/questions/77994075/return-iasyncenumerable-with-azure-functions-on-net-8-isolated
namespace railwaychatbot.FunctionApp
{
    public class MotoreAgentFunction
    {
        private readonly ILogger<MotoreAgentFunction> _logger;
        private readonly IAIEngine _aiEngine;

        public MotoreAgentFunction(ILogger<MotoreAgentFunction> logger, IAIEngine aiEngine)
        {
            _logger = logger;
            _aiEngine = aiEngine;
        }

        [Function("MotoreAgentFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var modelId = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME");
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"); 

            _logger.LogInformation(modelId);

            ArgumentException.ThrowIfNullOrEmpty(modelId, nameof(modelId));
            ArgumentException.ThrowIfNullOrEmpty(endpoint, nameof(endpoint));
            ArgumentException.ThrowIfNullOrEmpty(apiKey, nameof(apiKey)); 

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var history = JsonSerializer.Deserialize<ChatHistory>(requestBody);

            _logger.LogInformation(requestBody);

             

            var data = _aiEngine.InvokeMotoreOrarioAgentStreaming(history);

            var response = req.HttpContext.Response;
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "application/json";

            await foreach (var chunk in data)
            {
                await response.WriteAsync($"{JsonSerializer.Serialize(chunk)}\r\n");
                await response.Body.FlushAsync();
            }

            return new EmptyResult();
        }


    }
}
