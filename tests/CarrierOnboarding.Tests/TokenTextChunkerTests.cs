using CarrierOnboarding.Core.Abstractions;
using CarrierOnboarding.Core.Ingestion;
using Xunit;

namespace CarrierOnboarding.Tests;

public class TokenTextChunkerTests
{
    private static ExtractedDocument Doc(string text, int? page = 1) => new()
    {
        FileName = "guide.md",
        Pages = new[] { new ExtractedPage { Text = text, Page = page } },
    };

    [Fact]
    public void Chunk_SplitsLongText_IntoMultipleChunks()
    {
        var body = string.Join("\n", Enumerable.Range(0, 400).Select(i => $"Line number {i} with some filler words here."));
        var chunker = new TokenTextChunker(new ChunkingOptions { MaxTokens = 200, OverlapTokens = 20 });

        var chunks = chunker.Chunk(Doc(body), "Acme");

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.Equal("Acme", c.Carrier));
        Assert.All(chunks, c => Assert.Equal("guide.md", c.Source));
    }

    [Fact]
    public void Chunk_TracksNearestMarkdownHeading_AsSection()
    {
        var text = "# Authentication\n" + string.Join("\n", Enumerable.Repeat("Use an API key in the header.", 60));
        var chunker = new TokenTextChunker(new ChunkingOptions { MaxTokens = 120, OverlapTokens = 10 });

        var chunks = chunker.Chunk(Doc(text), "Acme");

        Assert.Contains(chunks, c => c.Section == "Authentication");
    }

    [Fact]
    public void Chunk_PopulatesPageNumber_ForCitations()
    {
        var chunker = new TokenTextChunker();
        var chunks = chunker.Chunk(Doc("Sandbox base url is https://sandbox.example.com", page: 7), "Acme");

        Assert.Single(chunks);
        Assert.Equal(7, chunks[0].Page);
        Assert.Contains("p. 7", chunks[0].Citation);
    }

    [Fact]
    public void Chunk_EmptyDocument_ProducesNoChunks()
    {
        var chunker = new TokenTextChunker();
        var chunks = chunker.Chunk(Doc("   \n  \n"), "Acme");
        Assert.Empty(chunks);
    }
}
