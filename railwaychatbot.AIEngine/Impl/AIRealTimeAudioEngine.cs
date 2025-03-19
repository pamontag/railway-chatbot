using Azure.AI.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.RealtimeConversation;
using railwaychatbot.AIEngine.Plugins;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
#pragma warning disable OPENAI002
namespace railwaychatbot.AIEngine.Impl
{
    // https://github.com/Azure/app-service-linux-docs/blob/master/HowTo/WebSockets/use_websockets_with_dotnet.md
    // https://github.com/Azure-Samples/aoai-realtime-audio-sdk/blob/main/dotnet/samples/console-from-mic/Program.cs
    public class AIRealTimeAudioEngine : IAIRealTimeAudioEngine
    {
        private const string tool_finish_conversation_name = "user_wants_to_finish_conversation";
        private Kernel _kernel;
        private string _modelRealTimeAudioId;
        private string _endpoint;
        private string _apiKey;
        private bool _isProcessing = true;

        RealtimeConversationSession _realtimeConversationClientSession;
        Dictionary<string, MemoryStream> outputAudioStreamsById = [];
        Dictionary<string, StringBuilder> functionArgumentBuildersById = [];

        

        // Define the event delegate
        public delegate void StreamingDeltaResponseHandler(BinaryData audioBytes);

        // Define the event
        public event StreamingDeltaResponseHandler? OnStreamingDeltaResponse;

        public AIRealTimeAudioEngine(string modelRealTimeAudioId, string endpoint, string apiKey)
        {
            _modelRealTimeAudioId = modelRealTimeAudioId;
            _endpoint = endpoint;
            _apiKey = apiKey;
        }

        public bool IsProcessing() { return _isProcessing; }

        public async Task InitSession()
        {
            _realtimeConversationClientSession = await GetConfiguredClientForAzureOpenAIWithKeyAsync(_endpoint, _modelRealTimeAudioId, _apiKey);
            // Start the background task to process conversation updates
            _ = Task.Run(() => ProcessConversationUpdatesAsync(false));
        }

        public async Task SendAudioAsync(Stream audio)
        {
            if(_realtimeConversationClientSession == null)
            {
                await InitSession();
            }  
            await _realtimeConversationClientSession.SendInputAudioAsync(audio);
        }

        public async IAsyncEnumerable<Stream> GetSingleResponseFromAudio(Stream audio)
        {
            await InitSession();
            await _realtimeConversationClientSession.SendInputAudioAsync(audio);


            await ProcessConversationUpdatesAsync(true);

            // Output the size of received audio data and dispose streams.
            foreach ((string itemId, Stream outputAudioStream) in outputAudioStreamsById)
            {
                Console.WriteLine($"Raw audio output for {itemId}: {outputAudioStream.Length} bytes");

                yield return outputAudioStream;
            }
        }

        private async Task ProcessConversationUpdatesAsync(bool singleResponse)
        {
            await foreach (ConversationUpdate update in _realtimeConversationClientSession.ReceiveUpdatesAsync())
            {
                // Notification indicating the start of the conversation session.
                if (update is ConversationSessionStartedUpdate sessionStartedUpdate)
                {
                    Console.WriteLine($"<<< Session started. ID: {sessionStartedUpdate.SessionId}");
                    Console.WriteLine();
                }

                // Notification indicating the start of detected voice activity.
                if (update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
                {
                    Console.WriteLine(
                        $"  -- Voice activity detection started at {speechStartedUpdate.AudioStartTime}");
                }

                // Notification indicating the end of detected voice activity.
                if (update is ConversationInputSpeechFinishedUpdate speechFinishedUpdate)
                {
                    Console.WriteLine(
                        $"  -- Voice activity detection ended at {speechFinishedUpdate.AudioEndTime}");
                }

                // Notification indicating the start of item streaming, such as a function call or response message.
                if (update is ConversationItemStreamingStartedUpdate itemStreamingStartedUpdate)
                {
                    Console.WriteLine("  -- Begin streaming of new item");
                    if (!string.IsNullOrEmpty(itemStreamingStartedUpdate.FunctionName))
                    {
                        Console.Write($"    {itemStreamingStartedUpdate.FunctionName}: ");
                    }
                }

                // Notification about item streaming delta, which may include audio transcript, audio bytes, or function arguments.
                if (update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
                {
                    Console.Write(deltaUpdate.AudioTranscript);
                    Console.Write(deltaUpdate.Text);
                    Console.Write(deltaUpdate.FunctionArguments);

                    // Handle audio bytes.
                    if (deltaUpdate.AudioBytes is not null)
                    {
                        if (!outputAudioStreamsById.TryGetValue(deltaUpdate.ItemId, out MemoryStream? value))
                        {
                            value = new MemoryStream();
                            outputAudioStreamsById[deltaUpdate.ItemId] = value;
                        }

                        value.Write(deltaUpdate.AudioBytes);
                        // HERE SEND THE AUDIO BYTES
                        OnStreamingDeltaResponse?.Invoke(deltaUpdate.AudioBytes);
                    }

                    // Handle function arguments.
                    if (!functionArgumentBuildersById.TryGetValue(deltaUpdate.ItemId, out StringBuilder? arguments))
                    {
                        functionArgumentBuildersById[deltaUpdate.ItemId] = arguments = new();
                    }

                    if (!string.IsNullOrWhiteSpace(deltaUpdate.FunctionArguments))
                    {
                        arguments.Append(deltaUpdate.FunctionArguments);
                    }
                }

                // Notification indicating the end of item streaming, such as a function call or response message.
                // At this point, audio transcript can be displayed on console, or a function can be called with aggregated arguments.
                if (update is ConversationItemStreamingFinishedUpdate itemStreamingFinishedUpdate)
                {
                    Console.WriteLine();
                    Console.WriteLine($"  -- Item streaming finished, item_id={itemStreamingFinishedUpdate.ItemId}");

                    if (itemStreamingFinishedUpdate.FunctionName == tool_finish_conversation_name)
                    {
                        Console.WriteLine($" <<< Finish tool invoked -- ending conversation!");
                        _isProcessing = false;
                        break;
                    }

                    // If an item is a function call, invoke a function with provided arguments.
                    if (itemStreamingFinishedUpdate.FunctionCallId is not null)
                    {
                        Console.WriteLine($"    + Responding to tool invoked by item: {itemStreamingFinishedUpdate.FunctionName}");

                        // Parse function name.
                        var (functionName, pluginName) = ParseFunctionName(itemStreamingFinishedUpdate.FunctionName);

                        // Deserialize arguments.
                        var argumentsString = functionArgumentBuildersById[itemStreamingFinishedUpdate.ItemId].ToString();
                        var arguments = DeserializeArguments(argumentsString);

                        // Create a function call content based on received data.
                        var functionCallContent = new FunctionCallContent(
                            functionName: functionName,
                            pluginName: pluginName,
                            id: itemStreamingFinishedUpdate.FunctionCallId,
                            arguments: arguments);

                        // Invoke a function.
                        var resultContent = await functionCallContent.InvokeAsync(_kernel);

                        // Create a function call output conversation item with function call result.
                        ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(
                            callId: itemStreamingFinishedUpdate.FunctionCallId,
                            output: ProcessFunctionResult(resultContent.Result));

                        // Send function call output conversation item to the session, so the model can use it for further processing.
                        await _realtimeConversationClientSession.AddItemAsync(functionOutputItem);
                    }
                    // If an item is a response message, output it to the console.
                    else if (itemStreamingFinishedUpdate.MessageContentParts?.Count > 0)
                    {
                        Console.Write($"    + [{itemStreamingFinishedUpdate.MessageRole}]: ");

                        foreach (ConversationContentPart contentPart in itemStreamingFinishedUpdate.MessageContentParts)
                        {
                            Console.Write(contentPart.AudioTranscript);
                        }

                        Console.WriteLine();
                    }

                    
                }

                // Notification indicating the completion of transcription from input audio.
                if (update is ConversationInputTranscriptionFinishedUpdate transcriptionCompletedUpdate)
                {
                    Console.WriteLine();
                    Console.WriteLine($"  -- User audio transcript: {transcriptionCompletedUpdate.Transcript}");
                    Console.WriteLine();
                }

                // Notification about completed model response turn.
                if (update is ConversationResponseFinishedUpdate turnFinishedUpdate)
                {
                    
                    Console.WriteLine($"  -- Model turn generation finished - Status: {turnFinishedUpdate.Status}");

                    // If the created session items contain a function name, it indicates a function call result has been provided,
                    // and response updates can begin.
                    if (turnFinishedUpdate.CreatedItems.Any(item => item.FunctionName?.Length > 0))
                    {
                        Console.WriteLine("  -- Ending client turn for pending tool responses");

                        await _realtimeConversationClientSession.StartResponseAsync();
                    }
                    // Otherwise, the model's response is provided, signaling that updates can be stopped.
                    else
                    {
                        if (singleResponse)                        
                            break;
                    }
                    
                }

                // Notification about error in conversation session.
                if (update is ConversationErrorUpdate errorUpdate)
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine($" <<< ERROR: {errorUpdate.Message}");
                    Console.WriteLine(errorUpdate.GetRawContent().ToString());
                    _isProcessing = false;
                    break;
                }

                
            }

        }

        private async Task<RealtimeConversationSession> GetConfiguredClientForAzureOpenAIWithKeyAsync(
        string aoaiEndpoint,
        string? aoaiDeployment,
        string aoaiApiKey)
        {
            AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));
            // Build kernel.
            _kernel = Kernel.CreateBuilder().Build();

            // Import plugin.
            _kernel.ImportPluginFromType<MotoreOrarioPlugins>();

            var client = aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
            var session = client.StartConversationSessionAsync().Result;
            ConversationSessionOptions sessionOptions = new()
            {
                Voice = ConversationVoice.Alloy,
                InputAudioFormat = ConversationAudioFormat.Pcm16,
                OutputAudioFormat = ConversationAudioFormat.Pcm16,
                Instructions = "Sei un operatore esperto nel dare informazioni sui treni. Gli utenti possono chiederti informazioni solo riguardo questo tema.",
                InputTranscriptionOptions = new()
                {
                    Model = "whisper-1",
                },
            };

            // We'll add a simple function tool that enables the model to interpret user input to figure out when it
            // might be a good time to stop the interaction.
            ConversationFunctionTool finishConversationTool = new()
            {
                Name = tool_finish_conversation_name,
                Description = "Invoked when the user says goodbye, expresses being finished, or otherwise seems to want to stop the interaction.",
                Parameters = BinaryData.FromString("{}")
            };

            // Add plugins/function from kernel as session tools.
            foreach (var tool in ConvertFunctions(_kernel))
            {
                sessionOptions.Tools.Add(tool);
            }
            sessionOptions.Tools.Add(finishConversationTool);

            // If any tools are available, set tool choice to "auto".
            if (sessionOptions.Tools.Count > 0)
            {
                sessionOptions.ToolChoice = ConversationToolChoice.CreateAutoToolChoice();
            }

            // Configure session with defined options.
            await session.ConfigureSessionAsync(sessionOptions);


            return session;
        }

        #region Helpers

        /// <summary>Helper method to parse a function name for compatibility with Semantic Kernel plugins/functions.</summary>
        private static (string FunctionName, string? PluginName) ParseFunctionName(string fullyQualifiedName)
        {
            const string FunctionNameSeparator = "-";

            string? pluginName = null;
            string functionName = fullyQualifiedName;

            int separatorPos = fullyQualifiedName.IndexOf(FunctionNameSeparator, StringComparison.Ordinal);
            if (separatorPos >= 0)
            {
                pluginName = fullyQualifiedName.AsSpan(0, separatorPos).Trim().ToString();
                functionName = fullyQualifiedName.AsSpan(separatorPos + FunctionNameSeparator.Length).Trim().ToString();
            }

            return (functionName, pluginName);
        }

        /// <summary>Helper method to deserialize function arguments.</summary>
        private static KernelArguments? DeserializeArguments(string argumentsString)
        {
            var arguments = JsonSerializer.Deserialize<KernelArguments>(argumentsString);

            if (arguments is not null)
            {
                // Iterate over copy of the names to avoid mutating the dictionary while enumerating it
                var names = arguments.Names.ToArray();
                foreach (var name in names)
                {
                    arguments[name] = arguments[name]?.ToString();
                }
            }

            return arguments;
        }

        /// <summary>Helper method to process function result in order to provide it to the model as string.</summary>
        private static string? ProcessFunctionResult(object? functionResult)
        {
            if (functionResult is string stringResult)
            {
                return stringResult;
            }

            return JsonSerializer.Serialize(functionResult);
        }

        /// <summary>Helper method to convert Kernel plugins/function to realtime session conversation tools.</summary>
        private IEnumerable<ConversationTool> ConvertFunctions(Kernel kernel)
        {
            foreach (var plugin in kernel.Plugins)
            {
                var functionsMetadata = plugin.GetFunctionsMetadata();

                foreach (var metadata in functionsMetadata)
                {
                    var toolDefinition = metadata.ToOpenAIFunction().ToFunctionDefinition(false);

                    yield return new ConversationFunctionTool()
                    {
                        Name = toolDefinition.FunctionName,
                        Description = toolDefinition.FunctionDescription,
                        Parameters = toolDefinition.FunctionParameters
                    };
                }
            }
        }

        #endregion
    }
}
