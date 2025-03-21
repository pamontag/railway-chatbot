﻿// Copyright (c) Microsoft. All rights reserved.

// See https://aka.ms/new-console-template for more information
// Import packages
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

using DotNetEnv;
using railwaychatbot.AIEngine.Plugins;
using railwaychatbot.AIEngine;
using railwaychatbot.AIEngine.Impl;
using railwaychatbot.ConsoleApp;
using System.Text.Json;
using System.Text;
using NAudio;
using NAudio.Wave;
using railwaychatbot.ConsoleApp.AudioSupport;
using Sprache;
using Azure;

string projectRoot;

Console.WriteLine("Application starts");

// Get the base directory
DirectoryInfo baseDirectory = new(AppDomain.CurrentDomain.BaseDirectory);

// retrieve the project root folder
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable SKEXP0001
if (baseDirectory.Parent.Parent.Name == "bin")
{
    projectRoot = baseDirectory.Parent.Parent.Parent.FullName;
}
else
{
    projectRoot = baseDirectory.FullName;
}
#pragma warning restore CS8602 // Dereference of a possibly null reference.

string envFilePath = Path.Combine(projectRoot, "./config/credentials.env");
Console.WriteLine($"envFilePath: {envFilePath}");

// Check for existence of evnFilePath
if (!File.Exists(envFilePath))
{
    Console.WriteLine($"File not found: {envFilePath}");
    throw new FileNotFoundException($"File not found: {envFilePath}");
}

Console.WriteLine("Select the strategy to use:");
Console.WriteLine("1. MotoreOrarioAgent");
Console.WriteLine("2. MotoreOrarioStreamingAgent");
Console.WriteLine("3. MotoreOrarioGroupAgent");
Console.WriteLine("4. MotoreOrarioGroupStreamingAgent");
Console.WriteLine("5. MotoreOrarioStreamingAgentFunction");
Console.WriteLine("6. MotoreOrarioGroupStreamingAgentFunction");
Console.WriteLine("7. MotoreOrarioAudioToTextAgent");
Console.WriteLine("8. MotoreOrarioAudioToTextStreamingAgent");
Console.WriteLine("9. MotoreOrarioRealTimeAudioSingleResponse");
Console.WriteLine("10. MotoreOrarioRealTimeAudioContinuosStream");
var strategy = Console.ReadLine();
// check if the strategy input is in the enum Strategy
Strategy selectedStrategy;
if (!Enum.TryParse<Strategy>(strategy, out selectedStrategy))
{
    Console.WriteLine("Invalid strategy selected. Please select a valid strategy.");
    return;
}


// Load the environment variables from the .env file
Env.Load(envFilePath);

// Populate values from your OpenAI deployment
var modelId = Env.GetString("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME");
var modelRealTimeAudioId = Env.GetString("AZURE_OPENAI_REALTIMEAUDIO_DEPLOYMENT_NAME");
var endpoint = Env.GetString("AZURE_OPENAI_ENDPOINT");
var apiKey = Env.GetString("AZURE_OPENAI_API_KEY");
var functionMotoreAgentEndpoint = Env.GetString("AZURE_FUNCTION_MOTOREAGENTFUNCTION_ENDPOINT");
var functionMotoreAgentGroupEndpoint = Env.GetString("AZURE_FUNCTION_MOTOREAGENTGROUPFUNCTION_ENDPOINT");
HttpClient httpClient = new HttpClient();

Console.WriteLine($"AZURE_OPENAI_ENDPOINT: {endpoint}\nAZURE_OPENAI_CHAT_DEPLOYMENT_NAME: {modelId}");

IAIEngine aiengine = new AIEngine(modelId, endpoint, apiKey);
IAIRealTimeAudioEngine airealtimeaudioengine = new AIRealTimeAudioEngine(modelRealTimeAudioId, endpoint, apiKey);

// Create a history store the conversation
var history = new ChatHistory();
SpeakerOutput speakerOutput = new();
AudioService audioService = new AudioService();
// Initiate a back-and-forth chat
string? userInput;
byte[] audioStreamBytes = new byte[0];
do
#pragma warning disable CS8602 // Dereference of a possibly null reference.
{
    // Collect user input
    Console.Write("User > ");
    if (selectedStrategy == Strategy.MotoreOrarioAudioToTextAgent || selectedStrategy == Strategy.MotoreOrarioAudioToTextStreamingAgent)
    {
        Console.WriteLine("Press any key to start recording your voice...");
        Console.ReadKey();
        Console.WriteLine("Recording...");
        audioService.StartRecording();
        Console.WriteLine("(press any key to exit)");
        Console.ReadKey();
        var bytes = audioService.StopRecording();
        userInput = await aiengine.GetTextFromAudio(bytes);
        Console.WriteLine($"You said: {userInput}");
    }
    else if (selectedStrategy == Strategy.MotoreOrarioRealTimeAudioSingleResponse)
    {
        Console.WriteLine("Press any key to start recording your voice...");
        Console.ReadKey();
        Console.WriteLine("Recording...");
        audioService.StartRecording();
        Console.WriteLine("(press any key to exit)");
        Console.ReadKey();
        audioStreamBytes = audioService.StopRecording();
        userInput = "RECORDED";
    }
    else if (selectedStrategy == Strategy.MotoreOrarioRealTimeAudioContinuosStream)
    {
        userInput = "START RECORDING";
    }
    else
    {
        userInput = Console.ReadLine();
    }

    // Check if userInput is not null before adding it to the chat history
    if (userInput != null)
    {
        history.AddUserMessage(userInput);
        StringBuilder sb = new StringBuilder();

        switch (selectedStrategy)
        {
            case Strategy.MotoreOrarioAgent:
                await foreach (ChatMessageContent response in aiengine.InvokeMotoreOrarioAgent(history))
                {
                    Console.WriteLine($"{response.Content}");

                    // Add the message from the agent to the chat history
                    history.AddMessage(response.Role, response.Content ?? string.Empty);
                }
                break;
            case Strategy.MotoreOrarioStreamingAgent:
                await foreach (StreamingChatMessageContent response in aiengine.InvokeMotoreOrarioAgentStreaming(history))
                {
                    Console.Write($"{response.Content}");
                    sb.Append(response.Content);
                }
                // Add the message from the agent to the chat history
                history.AddMessage(AuthorRole.Assistant, sb.ToString() ?? string.Empty);
                Console.WriteLine();
                break;
            case Strategy.MotoreOrarioGroupAgent:
                List<ChatMessageContent> messages = new List<ChatMessageContent>(); 
                await foreach (ChatMessageContent response in aiengine.InvokeMotoreOrarioGroupAgent(history))
                {
                    messages.Add(response);        
                }
                var resp = messages.LastOrDefault();
                Console.WriteLine($"{resp.AuthorName.ToString()} - {resp.Content}");
                // Add the message from the agent to the chat history
                history.AddMessage(resp.Role, resp.Content ?? string.Empty);

                break;
            case Strategy.MotoreOrarioGroupStreamingAgent:
                await foreach (StreamingChatMessageContent response in aiengine.InvokeMotoreOrarioGroupAgentStreaming(history))
                {
                    Console.Write($"{response.Content}");
                    sb.Append(response.Content);
                }
                // Add the message from the agent to the chat history
                history.AddMessage(AuthorRole.Assistant, sb.ToString() ?? string.Empty);
                Console.WriteLine();
                break;
            case Strategy.MotoreOrarioStreamingAgentFunction:
                var message = new HttpRequestMessage(HttpMethod.Post, functionMotoreAgentEndpoint);
                message.Content = new StringContent(JsonSerializer.Serialize(history), Encoding.UTF8, "application/json");
                var httpresponse = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
                using (var responseStream = await httpresponse.Content.ReadAsStreamAsync())
                using (var streamReader = new StreamReader(responseStream))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = await streamReader.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var chunk = JsonSerializer.Deserialize<StreamingChatMessageContent>(line);
                            Console.Write($"{chunk.Content}");

                        }
                    }
                }
                history.AddMessage(AuthorRole.Assistant, sb.ToString() ?? string.Empty);
                Console.WriteLine();
                break;
            case Strategy.MotoreOrarioGroupStreamingAgentFunction:
                var messageGroup = new HttpRequestMessage(HttpMethod.Post, functionMotoreAgentGroupEndpoint);
                messageGroup.Content = new StringContent(JsonSerializer.Serialize(history), Encoding.UTF8, "application/json");
                var httpresponseGroup = await httpClient.SendAsync(messageGroup, HttpCompletionOption.ResponseHeadersRead);
                using (var responseStream = await httpresponseGroup.Content.ReadAsStreamAsync())
                using (var streamReader = new StreamReader(responseStream))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = await streamReader.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var chunk = JsonSerializer.Deserialize<StreamingChatMessageContent>(line);
                            Console.Write($"{chunk.Content}");
                            sb.Append(chunk.Content);
                        }
                    }
                    history.AddMessage(AuthorRole.Assistant, sb.ToString() ?? string.Empty);
                }
                Console.WriteLine();
                break;
            case Strategy.MotoreOrarioAudioToTextAgent:
                await foreach (ChatMessageContent response in aiengine.InvokeMotoreOrarioAgent(history))
                {
                    Console.WriteLine($"{response.Content}");
                    var audioStream = await aiengine.GetAudioFromText(response.Content);
                    audioService.PlayAudio(audioStream);
                    // Add the message from the agent to the chat history
                    history.AddMessage(response.Role, response.Content ?? string.Empty);
                }
                break;
            case Strategy.MotoreOrarioAudioToTextStreamingAgent:
                await foreach (StreamingChatMessageContent response in aiengine.InvokeMotoreOrarioGroupAgentStreaming(history))
                {
                    Console.Write($"{response.Content}");
                    sb.Append(response.Content);
                }
                var audioStreaming = await aiengine.GetAudioFromText(sb.ToString());
                audioService.PlayAudio(audioStreaming);
                history.AddMessage(AuthorRole.Assistant, sb.ToString() ?? string.Empty);
                Console.WriteLine();
                break;
            case Strategy.MotoreOrarioRealTimeAudioSingleResponse:
                using (var audioStream = new MemoryStream(audioStreamBytes))
                {
                    
                    await foreach (MemoryStream response in airealtimeaudioengine.GetSingleResponseFromAudio(audioStream))
                    {
                        speakerOutput.EnqueueForPlayback(new BinaryData(response.ToArray()));
                    }
                }
                break;
            case Strategy.MotoreOrarioRealTimeAudioContinuosStream:               

                airealtimeaudioengine.OnStreamingDeltaResponse += (BinaryData audioBytes) =>
                {
                    // Execute speakerOutput.EnqueueForPlayback with the stream
                    speakerOutput.EnqueueForPlayback(audioBytes);
                };

                _ = Task.Run(async () =>
                {
                    // NEED A BUTTON FOR START AND STOP RECORDING OR STOP LISTENING WHEN AUDIO IS OUTPUT
                    using MicrophoneAudioStream microphoneInput = MicrophoneAudioStream.Start();
                    Console.WriteLine($" >>> Listening to microphone input");
                    Console.WriteLine($" >>> (Just tell the app you're done to finish)");
                    Console.WriteLine();
                    await airealtimeaudioengine.SendAudioAsync(microphoneInput);
                });

                while (airealtimeaudioengine.IsProcessing())
                {
                    await Task.Delay(100); // Adjust the delay as needed
                    
                }
                userInput = null;
                break;
            default:
                Console.WriteLine("Invalid strategy selected. Please select a valid strategy.");
                break;

        }
    }
} while (!string.IsNullOrWhiteSpace(userInput) && !userInput.Trim().Equals("EXIT", StringComparison.OrdinalIgnoreCase));
#pragma warning restore CS8602 // Dereference of a possibly null reference.

Console.WriteLine("Application ends");
