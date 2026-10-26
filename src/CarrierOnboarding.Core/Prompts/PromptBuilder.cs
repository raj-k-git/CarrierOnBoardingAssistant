using System.Text;
using CarrierOnboarding.Core.Models;

namespace CarrierOnboarding.Core.Prompts;

/// <summary>
/// Builds the system/user prompts used across the tool. All prompts instruct the
/// model to use ONLY the retrieved chunks and to flag uncertainty, never to invent values.
/// </summary>
public static class PromptBuilder
{
    public const string AskSystemPrompt =
        "You are a senior .NET integration engineer helping onboard a shipping carrier. " +
        "Answer the question using ONLY the provided documentation excerpts. " +
        "Cite the section or page for every fact in the form [Section (p. N)]. " +
        "If the answer is not contained in the excerpts, reply exactly: \"not found in document\". " +
        "Never invent values.";

    public const string GenerateSystemPrompt =
        "You are a senior .NET integration engineer. Generate DRAFT C# code for a shipping carrier " +
        "integration that follows the provided EXAMPLE integration's interfaces, naming, logging and " +
        "exception patterns EXACTLY. Use only facts from the retrieved documentation excerpts. " +
        "Mark anything uncertain or missing from the docs with a // TODO: verify comment. " +
        "Never invent field names, endpoints, or values that are not in the excerpts. " +
        "Output only the code for the single requested file, with no markdown fences or commentary.";

    /// <summary>Renders retrieved chunks with their section titles for inclusion in a prompt.</summary>
    public static string RenderChunks(IReadOnlyList<RetrievedChunk> chunks)
    {
        var sb = new StringBuilder();
        var index = 1;
        foreach (var r in chunks)
        {
            sb.AppendLine($"--- Excerpt {index} | {r.Chunk.Citation} ---");
            sb.AppendLine(r.Chunk.Text.Trim());
            sb.AppendLine();
            index++;
        }

        return sb.ToString().TrimEnd();
    }

    public static string BuildAskUserPrompt(string question, IReadOnlyList<RetrievedChunk> chunks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Documentation excerpts:");
        sb.AppendLine(RenderChunks(chunks));
        sb.AppendLine();
        sb.AppendLine($"Question: {question}");
        return sb.ToString();
    }
}
