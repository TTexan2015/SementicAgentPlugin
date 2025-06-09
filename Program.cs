#pragma warning disable SKEXP0001
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using Plugins;


// Initialize the kernel configuration
var assistantKernelBuilder = Kernel.CreateBuilder();

// Retrieve OpenAI credentials from environment variables
string modelName = "gpt-4o-mini";
string serviceEndpoint = "https://openaiserviceautogen04.openai.azure.com/";
string openAiApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(openAiApiKey))
{
    throw new InvalidOperationException("OpenAI API key is missing or not set.");
}

// Add OpenAI chat completion support
assistantKernelBuilder.Services.AddAzureOpenAIChatCompletion(modelName, serviceEndpoint, openAiApiKey);

// Register optional plugins
assistantKernelBuilder.Plugins.AddFromType<TimePlugin>();

// Build the assistant kernel
var assistantKernel = assistantKernelBuilder.Build();

// Setup conversation context
var conversationContext = new ChatHistory();
conversationContext.AddSystemMessage(
    "You are a helpful assistant. You can use available plugins or external knowledge as needed. Please explain your reasoning in your responses."
);

// Obtain the chat service
var assistantChatService = assistantKernel.GetRequiredService<IChatCompletionService>();

// Configure function invocation behavior
var functionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

// Start the interactive assistant loop
while (true)
{
    Console.Write("User Input> ");
    var userInput = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(userInput) || userInput.ToLower() == "exit" || userInput.ToLower() == "quit") break;

    // Store user input in conversation history
    conversationContext.AddUserMessage(userInput);

    try
    {
        // Generate response asynchronously
        var responseStream = assistantChatService.GetStreamingChatMessageContentsAsync(conversationContext, functionSettings, assistantKernel);
        var isInitialToken = true;
        var completeResponse = "";

        // Print the response as it streams
        await foreach (var messagePart in responseStream)
        {
            if (isInitialToken)
            {
                Console.Write("Response> ");
                isInitialToken = false;
            }
            Console.Write(messagePart.Content);
            completeResponse += messagePart.Content;
        }
        Console.WriteLine();

        // Store the assistant response
        conversationContext.AddAssistantMessage(completeResponse);
    }
    catch (Exception error)
    {
        Console.WriteLine($"Error: {error.Message}");
    }
}
