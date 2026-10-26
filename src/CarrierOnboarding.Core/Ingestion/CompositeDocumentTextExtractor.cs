using CarrierOnboarding.Core.Abstractions;

namespace CarrierOnboarding.Core.Ingestion;

/// <summary>Dispatches extraction to the first registered extractor that can handle the file.</summary>
public sealed class CompositeDocumentTextExtractor : IDocumentTextExtractor
{
    private readonly IReadOnlyList<IDocumentTextExtractor> _extractors;

    public CompositeDocumentTextExtractor(IEnumerable<IDocumentTextExtractor> extractors)
        => _extractors = extractors.ToList();

    public bool CanHandle(string filePath) => _extractors.Any(e => e.CanHandle(filePath));

    public Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var extractor = _extractors.FirstOrDefault(e => e.CanHandle(filePath))
            ?? throw new NotSupportedException(
                $"No text extractor is registered for '{Path.GetExtension(filePath)}' files.");

        return extractor.ExtractAsync(filePath, cancellationToken);
    }
}
