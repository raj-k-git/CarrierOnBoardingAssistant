using CarrierOnboarding.Core.Models;

namespace CarrierOnboarding.Core.Abstractions;

/// <summary>Extracts, chunks, embeds and stores a carrier document.</summary>
public interface IIndexingService
{
    Task<IndexingResult> IndexAsync(
        string documentPath,
        string carrier,
        CancellationToken cancellationToken = default);
}

public sealed class IndexingResult
{
    public required string Carrier { get; init; }
    public required string Document { get; init; }
    public int ChunkCount { get; init; }
}

/// <summary>Answers a question grounded strictly in retrieved chunks.</summary>
public interface IAskService
{
    Task<AskResult> AskAsync(
        string carrier,
        string question,
        int topK = 5,
        CancellationToken cancellationToken = default);
}

public sealed class AskResult
{
    public required string Answer { get; init; }
    public required IReadOnlyList<RetrievedChunk> Sources { get; init; }
}
