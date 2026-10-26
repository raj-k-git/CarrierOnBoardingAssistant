namespace CarrierOnboarding.Core.Abstractions;

/// <summary>Produces embedding vectors for text. Implemented over the LLM provider.</summary>
public interface IEmbeddingService
{
    Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default);

    async Task<ReadOnlyMemory<float>> EmbedAsync(string input, CancellationToken cancellationToken = default)
        => (await EmbedAsync(new[] { input }, cancellationToken))[0];
}
