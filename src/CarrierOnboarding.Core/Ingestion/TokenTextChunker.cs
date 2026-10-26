using System.Text;
using System.Text.RegularExpressions;
using CarrierOnboarding.Core.Abstractions;
using CarrierOnboarding.Core.Models;

namespace CarrierOnboarding.Core.Ingestion;

/// <summary>Options controlling how documents are split into chunks.</summary>
public sealed class ChunkingOptions
{
    /// <summary>Target chunk size, in estimated tokens.</summary>
    public int MaxTokens { get; set; } = 800;

    /// <summary>Overlap between consecutive chunks, in estimated tokens.</summary>
    public int OverlapTokens { get; set; } = 100;
}

/// <summary>
/// Splits documents into ~<see cref="ChunkingOptions.MaxTokens"/> token chunks with
/// <see cref="ChunkingOptions.OverlapTokens"/> overlap, tracking the nearest section
/// heading as metadata so answers can be cited.
/// </summary>
public sealed class TokenTextChunker : ITextChunker
{
    private readonly ChunkingOptions _options;

    // Matches markdown headings (#, ##) or short ALL-CAPS / numbered heading lines.
    private static readonly Regex MarkdownHeading = new(@"^\s{0,3}#{1,6}\s+(?<title>.+?)\s*$", RegexOptions.Compiled);
    private static readonly Regex NumberedHeading = new(@"^\s*(?<num>\d+(\.\d+)*)[\.\)]?\s+(?<title>[A-Z].{0,80})$", RegexOptions.Compiled);

    public TokenTextChunker(ChunkingOptions? options = null) => _options = options ?? new ChunkingOptions();

    public IReadOnlyList<DocumentChunk> Chunk(ExtractedDocument document, string carrier)
    {
        var chunks = new List<DocumentChunk>();
        var ordinal = 0;

        foreach (var page in document.Pages)
        {
            var lines = page.Text.Replace("\r\n", "\n").Split('\n');
            string? currentSection = null;
            var buffer = new StringBuilder();
            string? bufferSection = null;

            void Flush()
            {
                var text = buffer.ToString().Trim();
                if (text.Length == 0)
                {
                    return;
                }

                chunks.Add(new DocumentChunk
                {
                    Id = $"{carrier}:{document.FileName}:{ordinal}",
                    Carrier = carrier,
                    Text = text,
                    Section = bufferSection,
                    Source = document.FileName,
                    Page = page.Page,
                    Ordinal = ordinal,
                });
                ordinal++;

                // Seed the next buffer with a token-based overlap tail of the current chunk.
                var overlap = TakeOverlap(text, _options.OverlapTokens);
                buffer.Clear();
                buffer.Append(overlap);
                if (overlap.Length > 0)
                {
                    buffer.Append('\n');
                }
            }

            foreach (var raw in lines)
            {
                var line = raw.TrimEnd();
                var heading = DetectHeading(line);
                if (heading is not null)
                {
                    currentSection = heading;
                }

                if (buffer.Length == 0)
                {
                    bufferSection = currentSection;
                }

                buffer.Append(line).Append('\n');

                if (TokenEstimator.EstimateTokens(buffer.ToString()) >= _options.MaxTokens)
                {
                    Flush();
                    bufferSection = currentSection;
                }
            }

            Flush();
        }

        return chunks;
    }

    private static string? DetectHeading(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var md = MarkdownHeading.Match(line);
        if (md.Success)
        {
            return md.Groups["title"].Value.Trim();
        }

        var numbered = NumberedHeading.Match(line);
        if (numbered.Success)
        {
            return numbered.Value.Trim();
        }

        // Short, title-ish line with no sentence-ending punctuation.
        var trimmed = line.Trim();
        if (trimmed.Length is > 0 and <= 60
            && !trimmed.EndsWith('.')
            && !trimmed.EndsWith(',')
            && char.IsLetter(trimmed[0])
            && trimmed == trimmed.ToUpperInvariant()
            && trimmed.Any(char.IsLetter))
        {
            return trimmed;
        }

        return null;
    }

    private static string TakeOverlap(string text, int overlapTokens)
    {
        if (overlapTokens <= 0)
        {
            return string.Empty;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tail = new StringBuilder();
        for (var i = words.Length - 1; i >= 0; i--)
        {
            if (TokenEstimator.EstimateTokens(words[i] + " " + tail) > overlapTokens)
            {
                break;
            }

            tail.Insert(0, words[i] + " ");
        }

        return tail.ToString().Trim();
    }
}
