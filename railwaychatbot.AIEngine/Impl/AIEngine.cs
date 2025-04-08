using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToAudio;
using OpenAI;
using OpenAI.Audio;
using OpenAI.RealtimeConversation;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using railwaychatbot.AIEngine.Plugins.Impl;
using System.ClientModel;
using System.Text;
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable OPENAI002
namespace railwaychatbot.AIEngine.Impl
{
    public class AIEngine : IAIEngine
    {

        private readonly Kernel _kernel;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly AzureOpenAIClient _azureOpenAiClient;
        private readonly ChatCompletionAgent _motoreOrarioAgent;

        private AgentGroupChat _motoreOrarioGroupAgent;
        private const string AUDIO_TO_TEXT_MODEL = "whisper";
        private const string TEXT_TO_AUDIO_MODEL = "tts";

        private const string conversationalAgentName = "motore_orario_conversational_agent";
        private const string stationExpertAgentName = "motore_orario_station_expert_agent";
        private const string trainScheduleExpertAgentName = "motore_orario_train_schedule_expert_agent";
        private const string trainManagerAgentName = "motore_orario_train_manager_agent";

        private const bool ENABLE_OPENTELEMETRY_TRACING = false;

        public AIEngine(Kernel kernel, ICosmosDbService cosmosDbService, AzureOpenAIClient azureOpenAIClient)
        {
            // Build the kernel
            _kernel = kernel;
            _cosmosDbService = cosmosDbService;

            _azureOpenAiClient = azureOpenAIClient;

            _motoreOrarioAgent = CreateMotoreOrarioAgent(_kernel);
            _motoreOrarioGroupAgent = CreateMotoreOrarioAgentGroup(_kernel);
        }

        public async IAsyncEnumerable<ChatMessageContent> InvokeMotoreOrarioAgent(string text, string sessionId)
        {
            ChatHistory history = await GetChatHistory(sessionId);
            history.AddUserMessage(text);
            var chatMessages = _motoreOrarioAgent.InvokeAsync(history);
            StringBuilder stringBuilder = new StringBuilder();
            await foreach (var chatMessageContent in chatMessages)
            {
                stringBuilder.Append(chatMessageContent.Content);
                yield return chatMessageContent;
            }
            await _cosmosDbService.AddMessageAsync(sessionId, text, "user");
            await _cosmosDbService.AddMessageAsync(sessionId, stringBuilder.ToString(), "assistant");
        }


        public async IAsyncEnumerable<StreamingChatMessageContent> InvokeMotoreOrarioAgentStreaming(string text, string sessionId)
        {
            ChatHistory history = await GetChatHistory(sessionId);
            history.AddUserMessage(text);
            var chatMessages = _motoreOrarioAgent.InvokeStreamingAsync(history);
            StringBuilder stringBuilder = new StringBuilder();
            await foreach (var chatMessageContent in chatMessages)
            {
                stringBuilder.Append(chatMessageContent.Content);
                yield return chatMessageContent;
            }
            await _cosmosDbService.AddMessageAsync(sessionId, text, "user");
            await _cosmosDbService.AddMessageAsync(sessionId, stringBuilder.ToString(), "assistant");
        }

        public IAsyncEnumerable<ChatMessageContent> InvokeMotoreOrarioGroupAgent(ChatHistory history)
        {
            _motoreOrarioGroupAgent.IsComplete = false;
            _motoreOrarioGroupAgent.AddChatMessages(history);
            return _motoreOrarioGroupAgent.InvokeAsync();
        }
        public async IAsyncEnumerable<StreamingChatMessageContent> InvokeMotoreOrarioGroupAgentStreaming(ChatHistory history)
        {
            _motoreOrarioGroupAgent.IsComplete = false;
            _motoreOrarioGroupAgent.AddChatMessages(history);
            var data = _motoreOrarioGroupAgent.InvokeStreamingAsync();
            await foreach (var chunk in data)
            {
                // Check if the chunk is from the train manager agent, the only that has the role of wrap up the conversation to the user
                if (chunk.AuthorName == trainManagerAgentName)
                    yield return chunk;
            }
        }

        public async Task<string> GetTextFromAudio(byte[]? audio)
        {
            var audioClient = _azureOpenAiClient.GetAudioClient(AUDIO_TO_TEXT_MODEL);
            var sb = new StringBuilder();
            using (MemoryStream audioStream = new MemoryStream(audio))
            {
                audioStream.Position = 0;
                var result = await audioClient.TranscribeAudioAsync(audio: audioStream, audioFilename: "message.wav");
                foreach (var item in result.Value.Text)
                {
                    sb.Append(item);
                }
            }
            // Convert audio to text
            return sb.ToString();
        }

        public async Task<byte[]> GetAudioFromText(string message)
        {
            var audioClient = _azureOpenAiClient.GetAudioClient(TEXT_TO_AUDIO_MODEL);

            var result = await audioClient.GenerateSpeechAsync(message, GeneratedSpeechVoice.Alloy);

            return result.Value.ToArray();
        }

        public bool IsGroupChatComplete()
        {
            return _motoreOrarioGroupAgent.IsComplete;
        }

        private async Task<ChatHistory> GetChatHistory(string sessionId)
        {
            var messages = await _cosmosDbService.GetMessagesBySessionIdAsync(sessionId);
            ChatHistory history = new ChatHistory();
            foreach (var message in messages)
            {
                if (message.role == "user")
                {
                    history.AddUserMessage(message.message);
                }
                else if (message.role == "assistant")
                {
                    history.AddAssistantMessage(message.message);
                }
            }

            return history;
        }

        private ChatCompletionAgent CreateMotoreOrarioAgent(Kernel kernel)
        {
            // Clone kernel instance to allow for agent specific plug-in definition
            Kernel agentKernel = kernel.Clone();

            // Initialize plug-in from type
            // agentKernel.CreatePluginFromType<MotoreOrarioPlugins>();
            agentKernel.Plugins.AddFromType<MotoreOrarioMockPlugins>();

            // Create the agent
            return
                new ChatCompletionAgent()
                {
                    Name = "motore_orario_agent",
                    Instructions = @"Sei un operatore esperto nel dare informazioni sui treni.
                                    Gli utenti possono chiederti informazioni solo riguardo questo tema.",
                    Kernel = agentKernel,
                    Arguments = new KernelArguments(
                        new OpenAIPromptExecutionSettings()
                        {
                            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                        })
                };
        }
# pragma warning disable SKEXP0110
        private AgentGroupChat CreateMotoreOrarioAgentGroup(Kernel kernel)
        {


            Kernel conversationAgentKernel = kernel.Clone();
            Kernel stationExpertAgentKernel = kernel.Clone();
            Kernel trainScheduleExpertAgentKernel = kernel.Clone();

            conversationAgentKernel.Plugins.AddFromType<MotoreOrarioWeatherMockPlugin>();
            stationExpertAgentKernel.Plugins.AddFromType<MotoreOrarioStationExpertMockPlugin>();
            trainScheduleExpertAgentKernel.Plugins.AddFromType<MotoreOrarioTrainScheduleExpertMockPlugin>();

            var kernelArguments = new KernelArguments(
                    new OpenAIPromptExecutionSettings()
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                    });

            ChatCompletionAgent conversationalAgent = new ChatCompletionAgent()
            {
                Name = conversationalAgentName,
                Instructions = $$$"""
                                    Sei un operatore esperto nel dare informazioni sui treni e per dare supporto di primo livello. Non hai nessuna conoscenza sulle stazioni e sugli orari dei treni ma puoi rivoglerti agli altri agent per avere le informazioni.
                                    La data di oggi è: {{{DateTime.Now}}}. Fai riferimento a questa data quando non è specificata la data esatta ma solo riferimenti come oggi, ieri, l'altro ieri.
                                    Gli utenti possono chiederti informazioni solo riguardo questo tema.
                                    """,
                Kernel = conversationAgentKernel,
                Arguments = kernelArguments
            };

            ChatCompletionAgent stationExpertAgent = new ChatCompletionAgent()
            {
                Name = stationExpertAgentName,
                Instructions = $$$"""
                                    Sei un operatore esperto solo nel dare informazioni sulle stazioni ferroviarie. 
                                    La data di oggi è: {{{DateTime.Now}}}. Fai riferimento a questa data quando non è specificata la data esatta ma solo riferimenti come oggi, ieri, l'altro ieri.
                                    Gli utenti possono chiederti informazioni solo riguardo questo tema.
                                    """,
                Kernel = stationExpertAgentKernel,
                Arguments = kernelArguments
            };

            ChatCompletionAgent trainScheduleExpertAgent = new ChatCompletionAgent()
            {
                Name = trainScheduleExpertAgentName,
                Instructions = $$$"""
                                    Sei un operatore esperto solo nel dare informazioni sugli orari dei treni.La data di oggi è: {{{DateTime.Now}}}.
                                    Fai riferimento a questa data quando non è specificata la data esatta ma solo riferimenti come oggi, ieri, l'altro ieri.
                                    Gli utenti possono chiederti informazioni solo riguardo questo tema.
                                    """,
                Kernel = trainScheduleExpertAgentKernel,
                Arguments = kernelArguments
            };

            ChatCompletionAgent trainManagerAgent = new ChatCompletionAgent()
            {
                Name = trainManagerAgentName,
                Instructions = $$$"""
                                    Sei il manager della gestione ferriovaria .La data di oggi è: {{{DateTime.Now}}}.
                                    Fai riferimento a questa data quando non è specificata la data esatta ma solo riferimenti come oggi, ieri, l'altro ieri.
                                    Il tuo compito è validare il lavoro degli altri agenti. Devi essere certo che le informazioni siano tutte presenti e corrette in base alla domanda dell'utente.
                                    Se le risposte degli altri agenti sono esaurienti riassumi il contenuto in un unica risposta da fornire all'utente.
                                    """,
                Kernel = kernel,
                Arguments = kernelArguments
            };

            KernelFunction selectionFunction =
            AgentGroupChat.CreatePromptFunctionForStrategy(
                $$$"""  
                Il tuo compito è determinare quale partecipante avrà il turno successivo in una conversazione in base all'azione del partecipante più recente. 
                Indica solo il nome del partecipante che avrà il turno successivo.

                Scegli tra questi partecipanti:
                - {{{conversationalAgentName}}}
                - {{{stationExpertAgentName}}}
                - {{{trainScheduleExpertAgentName}}}
                - {{{trainManagerAgentName}}}

                Segui sempre questi passaggi quando selezioni il partecipante successivo:
                1) Dopo l'input dell'utente, è il turno di {{{conversationalAgentName}}} di iniziare le conversazioni e eventualmente rispondere su informazioni metereologiche.
                2) Se sono richieste informazioni sulle stazioni è il turno di {{{stationExpertAgentName}}}.
                3) Se sono richieste informazioni sullo scheduling dei treni è il turno di {{{trainScheduleExpertAgentName}}}.
                4) L'ultimo turno è di {{{trainManagerAgentName}}} di rivedere e approvare la risposta.
                5) Se la risposta viene approvata, la conversazione termina.
                6) Se il piano non viene approvato, è di nuovo il turno di {{{conversationalAgentName}}}.

                Se ti vengono fatte domande relative a più argomenti per i quali ci sono esperti diversi, 
                rispondi in base alle regole sopra indicate e contatta più agenti per formulare una risposta completa.                

                Storia:
                {{$history}}
                """,
                safeParameterNames: "history");

            const string TerminationToken = "yes";

            KernelFunction terminationFunction =
                AgentGroupChat.CreatePromptFunctionForStrategy(
                    $$$"""
                Determina se la risposta è soddisfacente.
                Se la risposta è soddisfacente rispondi con la singola parola {{{TerminationToken}}} senza nessuna altra spiegazione.

                Storia:
                {{$history}}
                """,
                safeParameterNames: "history");

            KernelFunctionSelectionStrategy selectionStrategy =
              new(selectionFunction, kernel)
              {
                  // The prompt variable name for the history argument.
                  HistoryVariableName = "history",
                  // Save tokens by not including the entire history in the prompt
                  HistoryReducer = new ChatHistoryTruncationReducer(3)
              };
            KernelFunctionTerminationStrategy terminationStrategy = new(terminationFunction, kernel)
            {
                // Parse the function response.
                ResultParser = (result) => result.GetValue<string>()?.Contains(TerminationToken, StringComparison.OrdinalIgnoreCase) ?? false,
                // The prompt variable name for the history argument.
                HistoryVariableName = "history",
                // Save tokens by not including the entire history in the prompt
                HistoryReducer = new ChatHistoryTruncationReducer(3),

                MaximumIterations = 10,

                Agents = [trainManagerAgent]
            };

            // seguire questa guida per rivedere il modello: https://www.developerscantina.com/p/semantic-kernel-multiagents/

            AgentGroupChat chat =
            new(conversationalAgent, stationExpertAgent, trainScheduleExpertAgent, trainManagerAgent)
            {
                ExecutionSettings = new()
                {
                    SelectionStrategy = selectionStrategy
                   ,
                    TerminationStrategy = terminationStrategy
                }
            };

            return chat;

        }

    }
}
