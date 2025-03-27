using railwaychatbot.AIEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace railwaychatbot.AIEngine
{
    public interface ICosmosDbService
    {
        Task AddMessageAsync(string sessionId, string message, string role);
        Task<List<ChatMessage>> GetMessagesBySessionIdAsync(string sessionId);
    }
}
