using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using railwaychatbot.AIEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace railwaychatbot.AIEngine.Impl
{
    public class CosmosDbService : ICosmosDbService
    {

        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseName;
        private readonly string _containerName;

        public CosmosDbService(CosmosClient cosmosClient, IConfiguration config)
        {
            _cosmosClient = cosmosClient;
            _databaseName = config["COSMOSDB_DATABASE"]!;
            _containerName = config["COSMOSDB_CHATCONTAINER"]!;
        }

        public async Task AddMessageAsync(string sessionId, string message, string role)
        {
            var chatMessage = new ChatMessage
            {
                id = Guid.NewGuid().ToString(),
                sessionid = sessionId,
                message = message,
                role = role,
                Timestamp = DateTime.UtcNow
            };

            var container = _cosmosClient.GetContainer(_databaseName, _containerName);
            await container.CreateItemAsync(chatMessage, new PartitionKey(chatMessage.id));
        }

        public async Task<List<ChatMessage>> GetMessagesBySessionIdAsync(string sessionId)
        {
            var container = _cosmosClient.GetContainer(_databaseName, _containerName);
            var query = container.GetItemQueryIterator<ChatMessage>(
                new QueryDefinition("SELECT * FROM c WHERE c.sessionid = @sessionId")
                .WithParameter("@sessionId", sessionId)
            );

            var messages = new List<ChatMessage>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                messages.AddRange(response);
            }

            return messages;
        }
    }
}
