namespace CarrierOnboarding.Core.Abstractions;

/// <summary>A single chat completion abstraction over the configured LLM provider.</summary>
public interface IChatService
{
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
