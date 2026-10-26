using CarrierOnboarding.Core.Abstractions;

namespace CarrierOnboarding.Core.Ingestion;

/// <summary>Extracts text from plain text and markdown files.</summary>
public sealed class PlainTextExtractor : IDocumentTextExtractor
{
    private static readonly string[] Extensions = { ".txt", ".md", ".markdown", ".text" };

    public bool CanHandle(string filePath)
        => Extensions.Any(e => filePath.EndsWith(e, StringComparison.OrdinalIgnoreCase));

    public async Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var text = await File.ReadAllTextAsync(filePath, cancellationToken);
        return new ExtractedDocument
        {
            FileName = Path.GetFileName(filePath),
            Pages = new[] { new ExtractedPage { Text = text, Page = null } },
        };
    }
}
