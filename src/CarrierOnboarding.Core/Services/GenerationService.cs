using System.Text;
using CarrierOnboarding.Core.Abstractions;
using CarrierOnboarding.Core.Models;
using CarrierOnboarding.Core.Prompts;
using Microsoft.Extensions.Logging;

namespace CarrierOnboarding.Core.Services;

/// <summary>
/// Generates DRAFT carrier integration files. Each file is produced by a focused LLM
/// call that includes example code and the relevant retrieved documentation excerpts.
/// This service NEVER deploys; it only writes draft files for engineer review.
/// </summary>
public sealed class GenerationService : IGenerationService
{
    private const int TopKPerTopic = 5;

    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _store;
    private readonly IChatService _chat;
    private readonly ILogger<GenerationService> _logger;

    public GenerationService(
        IEmbeddingService embeddings,
        IVectorStore store,
        IChatService chat,
        ILogger<GenerationService> logger)
    {
        _embeddings = embeddings;
        _store = store;
        _chat = chat;
        _logger = logger;
    }

    public async Task<GenerationResult> GenerateAsync(
        string carrier,
        string examplesFolder,
        string outputFolder,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(examplesFolder))
        {
            throw new DirectoryNotFoundException($"Examples folder not found: {examplesFolder}");
        }

        Directory.CreateDirectory(outputFolder);

        // 1. Run the fixed retrieval queries, one result set per topic.
        var topicChunks = new Dictionary<string, IReadOnlyList<RetrievedChunk>>();
        foreach (var (topic, query) in GenerationPrompts.RetrievalTopics)
        {
            var embedding = await _embeddings.EmbedAsync(query, cancellationToken);
            var chunks = await _store.SearchAsync(carrier, embedding, TopKPerTopic, cancellationToken);
            topicChunks[topic] = chunks;
            _logger.LogInformation("Topic '{Topic}' retrieved {Count} chunks", topic, chunks.Count);
        }

        if (topicChunks.Values.All(c => c.Count == 0))
        {
            throw new InvalidOperationException(
                $"No indexed content found for carrier '{carrier}'. Run the 'index' command first.");
        }

        // 2. Load 1-2 example integrations as few-shot pattern examples.
        var exampleCode = LoadExamples(examplesFolder);

        // 3. One focused LLM call per target file.
        var generated = new List<string>();
        foreach (var spec in GenerationPrompts.FileSpecs(carrier))
        {
            var chunks = Combine(topicChunks, spec.Topics);
            var userPrompt = GenerationPrompts.BuildFileUserPrompt(carrier, spec, exampleCode, chunks);
            _logger.LogInformation("Generating {File}", spec.FileName);

            var code = await _chat.CompleteAsync(PromptBuilder.GenerateSystemPrompt, userPrompt, cancellationToken);
            var path = Path.Combine(outputFolder, spec.FileName);
            await File.WriteAllTextAsync(path, StripFences(code), cancellationToken);
            generated.Add(path);
        }

        // 4. The mapping report with explicit OPEN QUESTIONS.
        var reportChunks = Combine(topicChunks, new[] { "request_fields", "response_format", "error_codes" });
        var reportPrompt = GenerationPrompts.BuildMappingReportUserPrompt(carrier, reportChunks);
        var report = await _chat.CompleteAsync(GenerationPrompts.MappingReportSystemPrompt, reportPrompt, cancellationToken);
        var reportPath = Path.Combine(outputFolder, "MAPPING_REPORT.md");
        await File.WriteAllTextAsync(reportPath, StripFences(report), cancellationToken);
        generated.Add(reportPath);

        return new GenerationResult
        {
            Carrier = carrier,
            OutputFolder = outputFolder,
            GeneratedFiles = generated,
        };
    }

    private static IReadOnlyList<RetrievedChunk> Combine(
        IReadOnlyDictionary<string, IReadOnlyList<RetrievedChunk>> topicChunks,
        IReadOnlyList<string> topics)
    {
        var seen = new HashSet<string>();
        var result = new List<RetrievedChunk>();
        foreach (var topic in topics)
        {
            if (!topicChunks.TryGetValue(topic, out var chunks))
            {
                continue;
            }

            foreach (var chunk in chunks)
            {
                if (seen.Add(chunk.Chunk.Id))
                {
                    result.Add(chunk);
                }
            }
        }

        return result;
    }

    private string LoadExamples(string examplesFolder)
    {
        var files = Directory
            .EnumerateFiles(examplesFolder, "*.cs", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .Take(2)
            .ToList();

        if (files.Count == 0)
        {
            _logger.LogWarning("No .cs example files found in {Folder}", examplesFolder);
            return "// (no example integration files were provided)";
        }

        var sb = new StringBuilder();
        foreach (var file in files)
        {
            sb.AppendLine($"// ===== Example: {Path.GetFileName(file)} =====");
            sb.AppendLine(File.ReadAllText(file));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string StripFences(string content)
    {
        var text = content.Trim();
        if (!text.StartsWith("```"))
        {
            return text;
        }

        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
        if (lines.Count > 0 && lines[0].StartsWith("```"))
        {
            lines.RemoveAt(0);
        }

        if (lines.Count > 0 && lines[^1].StartsWith("```"))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return string.Join("\n", lines).Trim();
    }
}
