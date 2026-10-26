namespace CarrierOnboarding.Core.Models;

/// <summary>A chunk returned from a similarity search together with its score.</summary>
public sealed class RetrievedChunk
{
    public required DocumentChunk Chunk { get; init; }

    /// <summary>Similarity score in the range [0, 1]; higher is more relevant.</summary>
    public double Score { get; init; }
}
