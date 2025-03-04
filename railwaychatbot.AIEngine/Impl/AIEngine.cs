using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using railwaychatbot.AIEngine.Plugins;

namespace railwaychatbot.AIEngine.Impl
{
    public class AIEngine : IAIEngine
    {

        private Kernel _kernel;
        private ChatCompletionAgent _motoreOrarioAgent;
#pragma warning disable SKEXP0110
        private AgentGroupChat _motoreOrarioGroupAgent;
        public AIEngine(string modelId, string endpoint, string apiKey)
        {
            // Create the kernel builder with the pointer to Azure OpenAI
            var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

            // Use the kernel builder to add enterprise components (for logging, in this case)
            builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Warning));

            // Build the kernel
            _kernel = builder.Build();

            _motoreOrarioAgent = CreateMotoreOrarioAgent(_kernel);
            _motoreOrarioGroupAgent = CreateMotoreOrarioAgentGroup(_kernel);
        }

        public IAsyncEnumerable<ChatMessageContent> InvokeMotoreOrarioAgent(ChatHistory history)
        {
            return _motoreOrarioAgent.InvokeAsync(history);
        }

        public IAsyncEnumerable<StreamingChatMessageContent> InvokeMotoreOrarioAgentStreaming(ChatHistory history)
        {
            return _motoreOrarioAgent.InvokeStreamingAsync(history);
        }

        public IAsyncEnumerable<ChatMessageContent> InvokeMotoreOrarioGroupAgent(ChatHistory history)
        {
            // _motoreOrarioGroupAgent.ResetAsync();
            _motoreOrarioGroupAgent.IsComplete = false;
            _motoreOrarioGroupAgent.AddChatMessages(history);
            return _motoreOrarioGroupAgent.InvokeAsync();
        }
        public IAsyncEnumerable<StreamingChatMessageContent> InvokeMotoreOrarioGroupAgentStreaming(ChatHistory history)
        {
            // _motoreOrarioGroupAgent.ResetAsync();
            _motoreOrarioGroupAgent.IsComplete = false;
            _motoreOrarioGroupAgent.AddChatMessages(history);
            return _motoreOrarioGroupAgent.InvokeStreamingAsync();
        }



        private ChatCompletionAgent CreateMotoreOrarioAgent(Kernel kernel)
        {
            // Clone kernel instance to allow for agent specific plug-in definition
            Kernel agentKernel = kernel.Clone();

            // Initialize plug-in from type
            // agentKernel.CreatePluginFromType<MotoreOrarioPlugins>();
            agentKernel.Plugins.AddFromType<MotoreOrarioPlugins>();

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
            var conversationalAgentName = "motore_orario_conversational_agent";
            var stationExpertAgentName = "motore_orario_station_expert_agent";
            var trainScheduleExpertAgentName = "motore_orario_train_schedule_expert_agent";

            Kernel conversationAgentKernel = kernel.Clone();
            Kernel stationExpertAgentKernel = kernel.Clone();
            Kernel trainScheduleExpertAgentKernel = kernel.Clone();

            conversationAgentKernel.Plugins.AddFromType<MotoreOrarioConversationalPlugins>();
            stationExpertAgentKernel.Plugins.AddFromType<MotoreOrarioStationExpertPlugins>();
            trainScheduleExpertAgentKernel.Plugins.AddFromType<MotoreOrarioTrainScheduleExpertPlugin>();

            var kernelArguments = new KernelArguments(
                    new OpenAIPromptExecutionSettings()
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                    });

            ChatCompletionAgent conversationalAgent = new ChatCompletionAgent()
            {
                Name = conversationalAgentName,
                Instructions = $$$"""
                                    Sei un operatore esperto nel dare informazioni sui treni. Non hai nessuna conoscenza sulle stazioni e sugli orari dei treni ma puoi rivoglerti agli altri agent per avere le informazioni.
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

#pragma warning disable SKEXP0110  
            KernelFunction selectionFunction =
            AgentGroupChat.CreatePromptFunctionForStrategy(
                $$$"""  
                Esamina la seguente storia di messaggi e scegli il prossimo partecipante in base all'argomento della conversazione.

                Scegli tra questi partecipanti:
                - {{{conversationalAgentName}}}
                - {{{stationExpertAgentName}}}
                - {{{trainScheduleExpertAgentName}}}

                Utilizza queste regole quando devi scegliere il prossimo partecipante:
                - Per questioni relative a stazioni è il turno di {{{stationExpertAgentName}}}.
                - Per questioni relative a orari dei treni è il turno di  {{{trainScheduleExpertAgentName}}}.
                - Per questioni relative al meteo e in qualsiasi altro caso risponde {{{conversationalAgentName}}}.

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
                Determina se la risposta è soddisfacente o se è necessario continuare la conversazione.
                Se la risposta è soddisfacente rispondi con una singola parola senza nessuna altra spiegazione: {{{TerminationToken}}}.
                In caso contrario contatta gli altri agenti per formulare una risposta completa.

                Storia:
                {{$history}}
                """,
                            safeParameterNames: "lastmessage");

#pragma warning disable SKEXP0001  
            KernelFunctionSelectionStrategy selectionStrategy =
              new(selectionFunction, kernel)
              {
                  // Parse the function response.
                  ResultParser = (result) => result.GetValue<string>() ?? "UNKNOWN",
                  // The prompt variable name for the history argument.
                  HistoryVariableName = "history",
                  // Save tokens by not including the entire history in the prompt
                  HistoryReducer = new ChatHistoryTruncationReducer(3),
              };
            KernelFunctionTerminationStrategy terminationStrategy = new(terminationFunction, kernel)
            {
                // Parse the function response.
                ResultParser = (result) => result.GetValue<string>()?.Contains(TerminationToken, StringComparison.OrdinalIgnoreCase) ?? false,
                // The prompt variable name for the history argument.
                HistoryVariableName = "history",
                // Save tokens by not including the entire history in the prompt
                HistoryReducer = new ChatHistoryTruncationReducer(3),

                MaximumIterations = 4,

                Agents = [conversationalAgent, stationExpertAgent, trainScheduleExpertAgent]
            };


            AgentGroupChat chat =
            new(conversationalAgent, stationExpertAgent, trainScheduleExpertAgent)
            {
                ExecutionSettings = new() { 
                    SelectionStrategy = selectionStrategy
                   // , TerminationStrategy = terminationStrategy 
                }
            };

            return chat;

        }

    }
}
