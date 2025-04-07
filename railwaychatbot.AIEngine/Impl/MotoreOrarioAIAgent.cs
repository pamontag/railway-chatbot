using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Audio;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using railwaychatbot.AIEngine.Plugins;
using Microsoft.Extensions.Configuration;

namespace railwaychatbot.AIEngine.Impl
{
    public class MotoreOrarioAIAgent : IMotoreOrarioAIAgent
    {
        private readonly Kernel _kernel;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly AzureOpenAIClient _azureOpenAiClient;
        private readonly ChatCompletionAgent _motoreOrarioAgent;
        private readonly string _prompt_yaml_path;
        private AgentThread _chatHistoryAgentThread;

        private const string AUDIO_TO_TEXT_MODEL = "whisper";
        private const string TEXT_TO_AUDIO_MODEL = "tts";

        public MotoreOrarioAIAgent(Kernel kernel, ICosmosDbService cosmosDbService, AzureOpenAIClient azureOpenAIClient, IConfiguration config)
        {
            // Build the kernel
            _kernel = kernel;
            _cosmosDbService = cosmosDbService;

            _azureOpenAiClient = azureOpenAIClient;
            _chatHistoryAgentThread = new ChatHistoryAgentThread();
            _prompt_yaml_path = config["PROMPT_MASTER_AGENT_YAML_PATH"]!;
            if(string.IsNullOrWhiteSpace(_prompt_yaml_path))
            {
                throw new ArgumentNullException("PROMPT_MASTER_AGENT_YAML_PATH", "The path to the YAML file containing the prompt cannot be null or empty.");
            }
            _motoreOrarioAgent = CreateMotoreOrarioAgent();
            
        }

        public async IAsyncEnumerable<AgentResponseItem<ChatMessageContent>> InvokeMotoreOrarioAgent(string text, string sessionId)
        {
            ChatHistory history = await GetChatHistory(sessionId);
            history.AddUserMessage(text);
            var chatMessages = _motoreOrarioAgent.InvokeAsync(history, thread: _chatHistoryAgentThread);
            StringBuilder stringBuilder = new StringBuilder();
            await foreach (AgentResponseItem<ChatMessageContent> chatMessageContent in chatMessages)
            {
                stringBuilder.Append(chatMessageContent);
                _chatHistoryAgentThread = chatMessageContent.Thread;
                yield return chatMessageContent;
            }
            await _cosmosDbService.AddMessageAsync(sessionId, text, "user");
            await _cosmosDbService.AddMessageAsync(sessionId, stringBuilder.ToString(), "assistant");            
        }


        public async IAsyncEnumerable<AgentResponseItem<StreamingChatMessageContent>> InvokeMotoreOrarioAgentStreaming(string text, string sessionId)
        {
            ChatHistory history = await GetChatHistory(sessionId);
            history.AddUserMessage(text);
            var chatMessages = _motoreOrarioAgent.InvokeStreamingAsync(history, thread: _chatHistoryAgentThread);
            StringBuilder stringBuilder = new StringBuilder();
            await foreach (AgentResponseItem<StreamingChatMessageContent> chatMessageContent in chatMessages)
            {
                stringBuilder.Append(chatMessageContent.Message);
                _chatHistoryAgentThread = chatMessageContent.Thread;
                yield return chatMessageContent;
            }
            await _cosmosDbService.AddMessageAsync(sessionId, text, "user");
            await _cosmosDbService.AddMessageAsync(sessionId, stringBuilder.ToString(), "assistant");
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

        private ChatCompletionAgent CreateMotoreOrarioAgent()
        {            

            // Initialize plug-in from type
            // agentKernel.CreatePluginFromType<MotoreOrarioPlugins>();
            _kernel.Plugins.AddFromType<MotoreOrarioPlugins>();

            string projectRoot;
            DirectoryInfo baseDirectory = new(AppDomain.CurrentDomain.BaseDirectory);
            projectRoot = baseDirectory.FullName;
            
            string yamlAgentPath = Path.Combine(projectRoot, _prompt_yaml_path);
            // Check for existence of evnFilePath
            if (!File.Exists(yamlAgentPath))
            {
                Console.WriteLine($"File not found: {yamlAgentPath}");
                throw new FileNotFoundException($"File not found: {yamlAgentPath}");
            }

            // Read YAML resource
            string generateStoryYaml = File.ReadAllText(yamlAgentPath);
            // Convert to a prompt template config
            PromptTemplateConfig templateConfig = KernelFunctionYaml.ToPromptTemplateConfig(generateStoryYaml);

            // Create agent with Instructions, Name and Description 
            // provided by the template config.
            ChatCompletionAgent agent =
                new(templateConfig,new KernelPromptTemplateFactory())
                {
                    Kernel = _kernel,
                    // Provide default values for template parameters
                    Arguments = new KernelArguments(templateConfig.DefaultExecutionSettings)
                    {
                        { "today", DateTime.Now.ToString("dd/MM/yyyy") }
                    }
                };
            return agent;
        }


    }
}
