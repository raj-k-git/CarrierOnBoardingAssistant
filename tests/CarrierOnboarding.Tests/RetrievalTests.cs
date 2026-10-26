using CarrierOnboarding.Core.Abstractions;
using CarrierOnboarding.Core.Ingestion;
using CarrierOnboarding.Core.Services;
using CarrierOnboarding.Core.Vector;
using CarrierOnboarding.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CarrierOnboarding.Tests;

public class RetrievalTests
{
    private static async Task<InMemoryVectorStore> BuildStoreAsync(IEmbeddingService embedder)
    {
        var store = new InMemoryVectorStore();
        var doc = new Core.Abstractions.ExtractedDocument
        {
            FileName = "guide.md",
            Pages = new[]
            {
                new Core.Abstractions.ExtractedPage
                {
                    Text = "# Authentication\nAuthenticate using an API key passed in the Authorization header.\n" +
                           "# Rate Limits\nThe API allows 100 requests per second before throttling.",
                    Page = 1,
                },
            },
        };

        var chunker = new TokenTextChunker(new ChunkingOptions { MaxTokens = 40, OverlapTokens = 5 });
        var chunks = chunker.Chunk(doc, "Acme");
        var embeddings = await embedder.EmbedAsync(chunks.Select(c => c.Text).ToList());
        await store.UpsertAsync(chunks.Select((c, i) => (c, embeddings[i])).ToList());
        return store;
    }

    [Fact]
    public async Task Search_ReturnsMostRelevantChunk_First()
    {
        IEmbeddingService embedder = new FakeEmbeddingService();
        var store = await BuildStoreAsync(embedder);

        var query = await embedder.EmbedAsync("how do I authenticate with an api key authorization header");
        var results = await store.SearchAsync("Acme", query, topK: 2);

        Assert.NotEmpty(results);
        Assert.Contains("Authenticate", results[0].Chunk.Text);
    }

    [Fact]
    public async Task Ask_WithNoIndexedContent_ReturnsNotFound()
    {
        var embedder = new FakeEmbeddingService();
        var store = new InMemoryVectorStore();
        var chat = new FakeChatService();
        var service = new AskService(embedder, store, chat, NullLogger<AskService>.Instance);

        var result = await service.AskAsync("Unknown", "anything?");

        Assert.Equal("not found in document", result.Answer);
        Assert.Empty(result.Sources);
    }

    [Fact]
    public async Task Ask_IncludesRetrievedChunksInUserPrompt()
    {
        var embedder = new FakeEmbeddingService();
        var store = await BuildStoreAsync(embedder);
        var chat = new FakeChatService();
        var service = new AskService(embedder, store, chat, NullLogger<AskService>.Instance);

        var result = await service.AskAsync("Acme", "what are the rate limits?", topK: 2);

        Assert.NotEmpty(result.Sources);
        Assert.Contains("Documentation excerpts", chat.LastUserPrompt);
        Assert.Contains("rate limits", chat.LastUserPrompt);
    }
}
