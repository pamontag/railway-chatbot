using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using railwaychatbot.AIEngine;
using System.Net;
using System.Text.Json;

namespace railwaychatbot.FunctionApp
{
    public class MotoreAgentGroupFunction
    {
        private readonly ILogger<MotoreAgentGroupFunction> _logger;
        private readonly IAIEngine _aiEngine;

        public MotoreAgentGroupFunction(ILogger<MotoreAgentGroupFunction> logger, IAIEngine aiengine)
        {
            _logger = logger;
            _aiEngine = aiengine;
        }

        [Function("MotoreAgentGroupFunction")]
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

             
            var response = req.HttpContext.Response;
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "application/json";
            try
            {
                var data = _aiEngine.InvokeMotoreOrarioGroupAgentStreaming(history);
                await foreach (var chunk in data)
                {
                    await response.WriteAsync($"{JsonSerializer.Serialize(chunk)}\r\n");
                    await response.Body.FlushAsync();
                }

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                var message = "Non sono riuscito a contattare i nostri esperti. Prova ancora.";
                await response.WriteAsync($"{JsonSerializer.Serialize(new StreamingChatMessageContent(AuthorRole.Assistant, message))}\r\n");
                await response.Body.FlushAsync();
            }
            return new EmptyResult();
        }
    }
}
