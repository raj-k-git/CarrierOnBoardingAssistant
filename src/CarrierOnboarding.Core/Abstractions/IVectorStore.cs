using CarrierOnboarding.Core.Models;

namespace CarrierOnboarding.Core.Abstractions;

/// <summary>
/// Stores embedded document chunks and performs similarity search. The default
/// implementation is in-memory; this interface allows swapping in SQLite or Qdrant later.
/// </summary>
public interface IVectorStore
{
    Task UpsertAsync(
        IReadOnlyList<(DocumentChunk Chunk, ReadOnlyMemory<float> Embedding)> items,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the <paramref name="topK"/> most similar chunks for the given carrier.</summary>
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string carrier,
        ReadOnlyMemory<float> queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default);

    /// <summary>Removes all chunks for a carrier (used when re-indexing).</summary>
    Task DeleteCarrierAsync(string carrier, CancellationToken cancellationToken = default);
}
