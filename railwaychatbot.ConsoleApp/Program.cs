// Copyright (c) Microsoft. All rights reserved.

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
using Azure;

string projectRoot;

Console.WriteLine("Application starts");

// Get the base directory
DirectoryInfo baseDirectory = new(AppDomain.CurrentDomain.BaseDirectory);

// retrieve the project root folder
#pragma warning disable CS8602 // Dereference of a possibly null reference.
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
Console.WriteLine("7. MotoreOrarioGroupAgentRealTimeAudio");
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

// Create a history store the conversation
var history = new ChatHistory();


// Initiate a back-and-forth chat
string? userInput;
do
#pragma warning disable CS8602 // Dereference of a possibly null reference.
{
    // Collect user input
    Console.Write("User > ");
    userInput = Console.ReadLine();

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
                await foreach (ChatMessageContent response in aiengine.InvokeMotoreOrarioGroupAgent(history))
                {
                    Console.WriteLine($"{response.Content}");
                    // Add the message from the agent to the chat history
                    history.AddMessage(response.Role, response.Content ?? string.Empty);
                }
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
            case Strategy.MotoreOrarioGroupAgentRealTimeAudio:
                throw new NotImplementedException();
                break;
            default:
                Console.WriteLine("Invalid strategy selected. Please select a valid strategy.");
                break;

        }
    }
} while (!string.IsNullOrWhiteSpace(userInput) && !userInput.Trim().Equals("EXIT", StringComparison.OrdinalIgnoreCase));
#pragma warning restore CS8602 // Dereference of a possibly null reference.

Console.WriteLine("Application ends");