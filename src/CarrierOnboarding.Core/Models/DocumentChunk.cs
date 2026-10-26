namespace CarrierOnboarding.Core.Models;

/// <summary>
/// A single chunk of carrier documentation together with the metadata needed
/// for retrieval and citation.
/// </summary>
public sealed class DocumentChunk
{
    public required string Id { get; init; }

    /// <summary>The carrier this chunk belongs to (used as a tag/filter).</summary>
    public required string Carrier { get; init; }

    /// <summary>The chunk text content.</summary>
    public required string Text { get; init; }

    /// <summary>Nearest section heading, used for citations.</summary>
    public string? Section { get; init; }

    /// <summary>Source document file name.</summary>
    public string? Source { get; init; }

    /// <summary>1-based page number when known (PDF), otherwise null.</summary>
    public int? Page { get; init; }

    /// <summary>Zero-based ordinal of this chunk within the document.</summary>
    public int Ordinal { get; init; }

    /// <summary>A short human readable citation such as "Authentication (p. 4)".</summary>
    public string Citation
    {
        get
        {
            var section = string.IsNullOrWhiteSpace(Section) ? Source ?? "document" : Section;
            return Page is { } p ? $"{section} (p. {p})" : section!;
        }
    }
}
