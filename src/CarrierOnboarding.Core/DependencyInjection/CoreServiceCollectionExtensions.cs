using CarrierOnboarding.Core.Abstractions;
using CarrierOnboarding.Core.Ingestion;
using CarrierOnboarding.Core.Services;
using CarrierOnboarding.Core.Vector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarrierOnboarding.Core.DependencyInjection;

/// <summary>
/// Registers the carrier-onboarding pipeline services. The LLM-provider-specific
/// services (<see cref="IEmbeddingService"/> and <see cref="IChatService"/>) must be
/// registered separately by the host, since they depend on configuration.
/// </summary>
public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCarrierOnboardingCore(
        this IServiceCollection services,
        ChunkingOptions? chunkingOptions = null,
        string? vectorStorePath = null)
    {
        services.AddSingleton<IDocumentTextExtractor, PdfTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, DocxTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, PlainTextExtractor>();
        services.AddSingleton<CompositeDocumentTextExtractor>();

        services.AddSingleton<ITextChunker>(_ => new TokenTextChunker(chunkingOptions));

        // In-memory by default; swap this single registration for SQLite/Qdrant later.
        services.AddSingleton<IVectorStore>(_ => new InMemoryVectorStore(vectorStorePath));

        services.AddSingleton<IIndexingService>(sp => new IndexingService(
            sp.GetRequiredService<CompositeDocumentTextExtractor>(),
            sp.GetRequiredService<ITextChunker>(),
            sp.GetRequiredService<IEmbeddingService>(),
            sp.GetRequiredService<IVectorStore>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<IndexingService>>()));

        services.AddSingleton<IAskService, AskService>();
        services.AddSingleton<IGenerationService, GenerationService>();
        services.AddSingleton<IEvaluationService, EvaluationService>();

        return services;
    }
}
