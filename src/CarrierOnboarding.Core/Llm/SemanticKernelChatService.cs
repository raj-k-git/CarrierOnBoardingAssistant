using CarrierOnboarding.Core.Abstractions;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CarrierOnboarding.Core.Llm;

/// <summary>Chat service backed by a Semantic Kernel chat completion service.</summary>
public sealed class SemanticKernelChatService : IChatService
{
    private readonly IChatCompletionService _inner;

    public SemanticKernelChatService(IChatCompletionService inner) => _inner = inner;

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userPrompt);

        var response = await _inner.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);
        return response.Content ?? string.Empty;
    }
}
