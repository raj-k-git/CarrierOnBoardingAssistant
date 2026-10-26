using System.Text.RegularExpressions;
using CarrierOnboarding.Core.Abstractions;

namespace CarrierOnboarding.Core.Services;

/// <summary>
/// Static comparison of a generated draft against a reference integration. We extract
/// candidate "fields" (property names and quoted field-like tokens) and "error codes"
/// (quoted upper/numeric tokens) from each code base and report matches and misses
/// relative to the hand-written reference.
/// </summary>
public sealed class EvaluationService : IEvaluationService
{
    private static readonly Regex PropertyName =
        new(@"public\s+[\w<>\?\[\]]+\s+(?<name>\w+)\s*\{\s*get", RegexOptions.Compiled);

    private static readonly Regex QuotedToken =
        new("\"(?<value>[^\"]{2,60})\"", RegexOptions.Compiled);

    // Error-code-like: all upper snake, or contains digits with letters, or pure digits 3-5 long.
    private static readonly Regex ErrorCodeLike =
        new(@"^([A-Z][A-Z0-9_]{2,}|[A-Z]+\d+|\d{3,5})$", RegexOptions.Compiled);

    public EvaluationResult Evaluate(string generatedFolder, string referenceFolder)
    {
        if (!Directory.Exists(generatedFolder))
        {
            throw new DirectoryNotFoundException($"Generated folder not found: {generatedFolder}");
        }

        if (!Directory.Exists(referenceFolder))
        {
            throw new DirectoryNotFoundException($"Reference folder not found: {referenceFolder}");
        }

        var generated = ReadCode(generatedFolder);
        var reference = ReadCode(referenceFolder);

        var genFields = ExtractFields(generated);
        var refFields = ExtractFields(reference);

        var genCodes = ExtractErrorCodes(generated);
        var refCodes = ExtractErrorCodes(reference);

        return new EvaluationResult
        {
            MatchedFields = refFields.Where(genFields.Contains).OrderBy(x => x).ToList(),
            MissedFields = refFields.Where(f => !genFields.Contains(f)).OrderBy(x => x).ToList(),
            MatchedErrorCodes = refCodes.Where(genCodes.Contains).OrderBy(x => x).ToList(),
            MissedErrorCodes = refCodes.Where(c => !genCodes.Contains(c)).OrderBy(x => x).ToList(),
        };
    }

    private static string ReadCode(string folder)
        => string.Join("\n",
            Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

    internal static HashSet<string> ExtractFields(string code)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in PropertyName.Matches(code))
        {
            set.Add(m.Groups["name"].Value);
        }

        foreach (Match m in QuotedToken.Matches(code))
        {
            var value = m.Groups["value"].Value;
            // Treat json-ish field names (identifier-like, no spaces) as candidate fields.
            if (Regex.IsMatch(value, @"^[A-Za-z][A-Za-z0-9_\-\.]{1,59}$") && !ErrorCodeLike.IsMatch(value))
            {
                set.Add(value);
            }
        }

        return set;
    }

    internal static HashSet<string> ExtractErrorCodes(string code)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in QuotedToken.Matches(code))
        {
            var value = m.Groups["value"].Value;
            if (ErrorCodeLike.IsMatch(value))
            {
                set.Add(value);
            }
        }

        return set;
    }
}
