using Azure.AI.OpenAI;
using OpenAI.RealtimeConversation;
using railwaychatbot.AIEngine.Plugins;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable OPENAI002
namespace railwaychatbot.AIEngine.Impl
{
    // https://github.com/Azure/app-service-linux-docs/blob/master/HowTo/WebSockets/use_websockets_with_dotnet.md
    // https://github.com/Azure-Samples/aoai-realtime-audio-sdk/blob/main/dotnet/samples/console-from-mic/Program.cs
    public class AIRealTimeAudioEngine
    {

        private RealtimeConversationSession _realtimeConversationClientSession;

        public AIRealTimeAudioEngine(string modelRealTimeAudioId, string endpoint, string apiKey)
        {
            _realtimeConversationClientSession = GetConfiguredClientForAzureOpenAIWithKey(endpoint, modelRealTimeAudioId, apiKey);
        }

        private static RealtimeConversationSession GetConfiguredClientForAzureOpenAIWithKey(
        string aoaiEndpoint,
        string? aoaiDeployment,
        string aoaiApiKey)
        {
            AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));
            MotoreOrarioFunctions functions = new MotoreOrarioFunctions();
            var client = aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
            var session = client.StartConversationSessionAsync().Result;
            session.ConfigureSessionAsync(new ConversationSessionOptions()
            {
                Tools = { functions.getStations },
                Instructions = "Sei un operatore esperto nel dare informazioni sui treni. Gli utenti possono chiederti informazioni solo riguardo questo tema.",
                InputTranscriptionOptions = new()
                {
                    Model = "whisper-1",
                },
            });


            return session;
        }
    }
}
