using CarrierOnboarding.Core.Abstractions;

namespace CarrierOnboarding.Tests.Fakes;

/// <summary>Chat stub that echoes the prompts it received, for prompt-wiring assertions.</summary>
public sealed class FakeChatService : IChatService
{
    public string? LastSystemPrompt { get; private set; }
    public string? LastUserPrompt { get; private set; }
    public string Response { get; set; } = "stubbed answer";

    public Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        LastSystemPrompt = systemPrompt;
        LastUserPrompt = userPrompt;
        return Task.FromResult(Response);
    }
}
