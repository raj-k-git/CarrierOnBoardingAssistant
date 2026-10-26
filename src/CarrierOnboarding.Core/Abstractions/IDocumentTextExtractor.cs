namespace CarrierOnboarding.Core.Abstractions;

/// <summary>Extracted text from a source document, optionally split by page.</summary>
public sealed class ExtractedDocument
{
    public required string FileName { get; init; }

    /// <summary>
    /// One entry per logical page. For formats without pages (txt, docx, md)
    /// this contains a single entry with <see cref="ExtractedPage.Page"/> == null.
    /// </summary>
    public required IReadOnlyList<ExtractedPage> Pages { get; init; }
}

public sealed class ExtractedPage
{
    public required string Text { get; init; }

    /// <summary>1-based page number, or null when the format has no pagination.</summary>
    public int? Page { get; init; }
}

/// <summary>Extracts plain text from a supported document format.</summary>
public interface IDocumentTextExtractor
{
    /// <summary>Returns true when this extractor can handle the given file.</summary>
    bool CanHandle(string filePath);

    Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken cancellationToken = default);
}
