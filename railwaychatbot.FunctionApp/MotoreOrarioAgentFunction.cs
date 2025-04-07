using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using railwaychatbot.AIEngine;
using railwaychatbot.AIEngine.Model;
using System.Net;

namespace railwaychatbot.FunctionApp
{
    public class MotoreOrarioAgentFunction
    {
        private readonly ILogger<MotoreAgentFunction> _logger;
        private readonly IMotoreOrarioAIAgent _aiEngine;

        public MotoreOrarioAgentFunction(ILogger<MotoreAgentFunction> logger, IMotoreOrarioAIAgent aiEngine)
        {
            _logger = logger;
            _aiEngine = aiEngine;
        }

        [Function("MotoreAgentFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var message = JsonSerializer.Deserialize<ChatMessage>(requestBody);

            _logger.LogInformation(requestBody);

            // da correggere la chiamata
            var data = _aiEngine.InvokeMotoreOrarioAgentStreaming(message.message, message.sessionid);

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
