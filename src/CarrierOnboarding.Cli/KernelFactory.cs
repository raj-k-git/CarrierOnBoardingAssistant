using CarrierOnboarding.Core.Abstractions;
using CarrierOnboarding.Core.Configuration;
using CarrierOnboarding.Core.Llm;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace CarrierOnboarding.Cli;

/// <summary>
/// Builds a Semantic Kernel configured for the selected provider (Azure OpenAI or
/// Ollama) and exposes the chat and embedding services behind the Core abstractions.
/// No API keys live in source: Azure keys come from config/user-secrets/env vars.
/// </summary>
public static class KernelFactory
{
    public static (IChatService Chat, IEmbeddingService Embeddings) Create(LlmOptions options)
    {
        var builder = Kernel.CreateBuilder();

        switch (options.Provider?.Trim().ToLowerInvariant())
        {
            case "azureopenai":
                ConfigureAzureOpenAI(builder, options.AzureOpenAI);
                break;

            case "ollama":
                ConfigureOllama(builder, options.Ollama);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown LLM provider '{options.Provider}'. Use 'AzureOpenAI' or 'Ollama'.");
        }

        var kernel = builder.Build();

        var chat = new SemanticKernelChatService(kernel.GetRequiredService<IChatCompletionService>());
        var embeddings = new SemanticKernelEmbeddingService(
            kernel.GetRequiredService<ITextEmbeddingGenerationService>());

        return (chat, embeddings);
    }

    private static void ConfigureAzureOpenAI(IKernelBuilder builder, AzureOpenAiOptions options)
    {
        var apiKey = options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Azure OpenAI API key not found. Set it via user-secrets (Llm:AzureOpenAI:ApiKey) " +
                "or the AZURE_OPENAI_API_KEY environment variable.");
        }

        builder.AddAzureOpenAIChatCompletion(
            deploymentName: options.ChatDeployment,
            endpoint: options.Endpoint,
            apiKey: apiKey);

        builder.AddAzureOpenAITextEmbeddingGeneration(
            deploymentName: options.EmbeddingDeployment,
            endpoint: options.Endpoint,
            apiKey: apiKey);
    }

    private static void ConfigureOllama(IKernelBuilder builder, OllamaOptions options)
    {
        var endpoint = new Uri(options.Endpoint);
        builder.AddOllamaChatCompletion(options.ChatModel, endpoint);
        builder.AddOllamaTextEmbeddingGeneration(options.EmbeddingModel, endpoint);
    }
}
