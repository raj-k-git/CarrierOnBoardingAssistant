using CarrierOnboarding.Core.Abstractions;

namespace CarrierOnboarding.Tests.Fakes;

/// <summary>
/// Deterministic, offline embedding stub. Produces a fixed-size bag-of-words vector so
/// that texts sharing vocabulary score higher under cosine similarity - enough to test
/// retrieval without calling a real LLM.
/// </summary>
public sealed class FakeEmbeddingService : IEmbeddingService
{
    private const int Dimensions = 256;

    public Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        var result = inputs.Select(Embed).ToList();
        return Task.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(result);
    }

    private static ReadOnlyMemory<float> Embed(string text)
    {
        var vector = new float[Dimensions];
        foreach (var token in Tokenize(text))
        {
            var bucket = (token.GetHashCode() & int.MaxValue) % Dimensions;
            vector[bucket] += 1f;
        }

        return vector;
    }

    private static IEnumerable<string> Tokenize(string text)
        => text.ToLowerInvariant()
            .Split(new[] { ' ', '\n', '\r', '\t', '.', ',', ':', ';', '(', ')', '/', '-' },
                StringSplitOptions.RemoveEmptyEntries);
}
