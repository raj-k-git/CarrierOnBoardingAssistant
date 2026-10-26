using CarrierOnboarding.Core.Models;
using CarrierOnboarding.Core.Prompts;
using Xunit;

namespace CarrierOnboarding.Tests;

public class PromptBuildingTests
{
    private static RetrievedChunk Chunk(string text, string section, int page) => new()
    {
        Chunk = new DocumentChunk
        {
            Id = $"Acme:guide:{page}",
            Carrier = "Acme",
            Text = text,
            Section = section,
            Source = "guide.md",
            Page = page,
            Ordinal = page,
        },
        Score = 0.9,
    };

    [Fact]
    public void RenderChunks_IncludesCitationsAndText()
    {
        var chunks = new[] { Chunk("Use an API key.", "Authentication", 4) };

        var rendered = PromptBuilder.RenderChunks(chunks);

        Assert.Contains("Authentication (p. 4)", rendered);
        Assert.Contains("Use an API key.", rendered);
    }

    [Fact]
    public void AskUserPrompt_ContainsQuestionAndExcerpts()
    {
        var chunks = new[] { Chunk("Rate limit is 100 rps.", "Rate Limits", 6) };

        var prompt = PromptBuilder.BuildAskUserPrompt("What is the rate limit?", chunks);

        Assert.Contains("What is the rate limit?", prompt);
        Assert.Contains("Rate limit is 100 rps.", prompt);
    }

    [Fact]
    public void AskSystemPrompt_InstructsGroundingAndNotFoundFallback()
    {
        Assert.Contains("not found in document", PromptBuilder.AskSystemPrompt);
        Assert.Contains("ONLY", PromptBuilder.AskSystemPrompt);
    }

    [Fact]
    public void FieldMappingPrompt_ListsCanonicalShipmentFields()
    {
        var spec = GenerationPrompts.FileSpecs("Acme").Single(f => f.FileName.Contains("FieldMapping"));
        var prompt = GenerationPrompts.BuildFileUserPrompt("Acme", spec, "// example", Array.Empty<RetrievedChunk>());

        Assert.Contains(ShipmentModel.Fields[0], prompt);
        Assert.Contains("TODO: verify", GenerationPrompts.FileSpecs("Acme")
            .First(f => f.FileName.Contains("FieldMapping")).Instruction);
    }

    [Fact]
    public void GenerateSystemPrompt_RequiresFollowingExampleAndFlaggingUncertainty()
    {
        Assert.Contains("EXACTLY", PromptBuilder.GenerateSystemPrompt);
        Assert.Contains("TODO: verify", PromptBuilder.GenerateSystemPrompt);
    }
}
