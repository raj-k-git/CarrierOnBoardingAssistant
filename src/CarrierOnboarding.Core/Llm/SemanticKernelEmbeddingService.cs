using CarrierOnboarding.Core.Abstractions;
using Microsoft.SemanticKernel.Embeddings;

namespace CarrierOnboarding.Core.Llm;

/// <summary>Embedding service backed by a Semantic Kernel text embedding generator.</summary>
public sealed class SemanticKernelEmbeddingService : IEmbeddingService
{
    private readonly ITextEmbeddingGenerationService _inner;

    public SemanticKernelEmbeddingService(ITextEmbeddingGenerationService inner) => _inner = inner;

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.GenerateEmbeddingsAsync(inputs.ToList(), cancellationToken: cancellationToken);
        return result as IReadOnlyList<ReadOnlyMemory<float>> ?? result.ToList();
    }
}
