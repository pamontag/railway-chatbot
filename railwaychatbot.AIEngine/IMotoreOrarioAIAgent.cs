using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Agents;

namespace railwaychatbot.AIEngine
{
    public interface IMotoreOrarioAIAgent
    {
        IAsyncEnumerable<AgentResponseItem<ChatMessageContent>> InvokeMotoreOrarioAgent(string text, string sessionId);
        IAsyncEnumerable<AgentResponseItem<StreamingChatMessageContent>> InvokeMotoreOrarioAgentStreaming(string text, string sessionId);
        Task<string> GetTextFromAudio(byte[]? audio);
        Task<byte[]> GetAudioFromText(string message);
    }
}
