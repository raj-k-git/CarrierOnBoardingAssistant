using CarrierOnboarding.Core.Abstractions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace CarrierOnboarding.Core.Ingestion;

/// <summary>Extracts text from PDF files, one <see cref="ExtractedPage"/> per page.</summary>
public sealed class PdfTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string filePath)
        => filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var pages = new List<ExtractedPage>();
        using (var document = PdfDocument.Open(filePath))
        {
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = ContentOrderTextExtractor.GetText(page);
                pages.Add(new ExtractedPage { Text = text ?? string.Empty, Page = page.Number });
            }
        }

        return Task.FromResult(new ExtractedDocument
        {
            FileName = Path.GetFileName(filePath),
            Pages = pages,
        });
    }
}
