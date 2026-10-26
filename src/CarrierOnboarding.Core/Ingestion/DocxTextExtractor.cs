using CarrierOnboarding.Core.Abstractions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace CarrierOnboarding.Core.Ingestion;

/// <summary>Extracts text from .docx Word documents.</summary>
public sealed class DocxTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string filePath)
        => filePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

    public Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var lines = new List<string>();
        using (var doc = WordprocessingDocument.Open(filePath, false))
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body is not null)
            {
                foreach (var paragraph in body.Descendants<Paragraph>())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var text = paragraph.InnerText;
                    lines.Add(text);
                }
            }
        }

        var page = new ExtractedPage { Text = string.Join("\n", lines), Page = null };
        return Task.FromResult(new ExtractedDocument
        {
            FileName = Path.GetFileName(filePath),
            Pages = new[] { page },
        });
    }
}
