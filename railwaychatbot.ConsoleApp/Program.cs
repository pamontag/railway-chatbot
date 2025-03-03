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

// Load the environment variables from the .env file
Env.Load(envFilePath);

// Populate values from your OpenAI deployment
var modelId = Env.GetString("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME");
var endpoint = Env.GetString("AZURE_OPENAI_ENDPOINT");
var apiKey = Env.GetString("AZURE_OPENAI_API_KEY");

Console.WriteLine($"AZURE_OPENAI_ENDPOINT: {endpoint}\nAZURE_OPENAI_CHAT_DEPLOYMENT_NAME: {modelId}");

IAIEngine aiengine = new AIEngine(modelId,endpoint,apiKey);

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
        // await foreach (StreamingChatMessageContent response in aiengine.InvokeMotoreOrarioGroupAgentStreaming(history))
        await foreach (ChatMessageContent response in aiengine.InvokeMotoreOrarioGroupAgent(history))
        {
            Console.WriteLine($"{response.Content}");
            
            // Add the message from the agent to the chat history
            history.AddMessage(response.Role, response.Content ?? string.Empty);
        }

    }
} while (!string.IsNullOrWhiteSpace(userInput) && !userInput.Trim().Equals("EXIT", StringComparison.OrdinalIgnoreCase));
#pragma warning restore CS8602 // Dereference of a possibly null reference.

Console.WriteLine("Application ends");