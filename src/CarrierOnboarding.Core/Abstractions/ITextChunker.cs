using CarrierOnboarding.Core.Models;

namespace CarrierOnboarding.Core.Abstractions;

/// <summary>Splits an extracted document into overlapping chunks for embedding.</summary>
public interface ITextChunker
{
    IReadOnlyList<DocumentChunk> Chunk(ExtractedDocument document, string carrier);
}
