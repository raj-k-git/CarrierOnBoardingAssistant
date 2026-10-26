using CarrierOnboarding.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CarrierOnboarding.Core.Services;

/// <summary>Extracts, chunks, embeds and stores a carrier document.</summary>
public sealed class IndexingService : IIndexingService
{
    private readonly IDocumentTextExtractor _extractor;
    private readonly ITextChunker _chunker;
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _store;
    private readonly ILogger<IndexingService> _logger;

    public IndexingService(
        IDocumentTextExtractor extractor,
        ITextChunker chunker,
        IEmbeddingService embeddings,
        IVectorStore store,
        ILogger<IndexingService> logger)
    {
        _extractor = extractor;
        _chunker = chunker;
        _embeddings = embeddings;
        _store = store;
        _logger = logger;
    }

    public async Task<IndexingResult> IndexAsync(
        string documentPath,
        string carrier,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(documentPath))
        {
            throw new FileNotFoundException("Document not found.", documentPath);
        }

        _logger.LogInformation("Extracting text from {Document}", documentPath);
        var document = await _extractor.ExtractAsync(documentPath, cancellationToken);

        var chunks = _chunker.Chunk(document, carrier);
        _logger.LogInformation("Produced {Count} chunks for carrier {Carrier}", chunks.Count, carrier);

        if (chunks.Count == 0)
        {
            return new IndexingResult { Carrier = carrier, Document = document.FileName, ChunkCount = 0 };
        }

        var embeddings = await _embeddings.EmbedAsync(chunks.Select(c => c.Text).ToList(), cancellationToken);

        // Re-index replaces any previous content for this document's carrier.
        await _store.DeleteCarrierAsync(carrier, cancellationToken);
        var items = chunks
            .Select((c, i) => (c, embeddings[i]))
            .ToList();
        await _store.UpsertAsync(items, cancellationToken);

        _logger.LogInformation("Stored {Count} embedded chunks for carrier {Carrier}", chunks.Count, carrier);
        return new IndexingResult { Carrier = carrier, Document = document.FileName, ChunkCount = chunks.Count };
    }
}
