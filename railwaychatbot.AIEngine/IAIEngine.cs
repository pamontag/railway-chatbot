using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;

namespace railwaychatbot.AIEngine
{
    public interface IAIEngine
    {
        IAsyncEnumerable<ChatMessageContent> InvokeMotoreOrarioAgent(ChatHistory history);
        IAsyncEnumerable<StreamingChatMessageContent> InvokeMotoreOrarioAgentStreaming(ChatHistory history);
        IAsyncEnumerable<ChatMessageContent> InvokeMotoreOrarioGroupAgent(ChatHistory history);
        IAsyncEnumerable<StreamingChatMessageContent> InvokeMotoreOrarioGroupAgentStreaming(ChatHistory history);
        Task<string> GetTextFromAudio(byte[]? audio);
        Task<byte[]> GetAudioFromText(string message);

        bool IsGroupChatComplete();
    }
}
