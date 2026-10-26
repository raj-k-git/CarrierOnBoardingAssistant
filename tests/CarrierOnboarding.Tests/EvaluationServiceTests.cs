using CarrierOnboarding.Core.Services;
using Xunit;

namespace CarrierOnboarding.Tests;

public class EvaluationServiceTests
{
    [Fact]
    public void ExtractErrorCodes_FindsQuotedCodeTokens()
    {
        var code = "if (err == \"AUTH_FAILED\") throw; var x = \"E1001\"; var n = \"4001\";";
        var codes = EvaluationService.ExtractErrorCodes(code);

        Assert.Contains("AUTH_FAILED", codes);
        Assert.Contains("E1001", codes);
        Assert.Contains("4001", codes);
    }

    [Fact]
    public void ExtractFields_FindsPropertyNames()
    {
        var code = "public string TrackingNumber { get; set; } public int Weight { get; init; }";
        var fields = EvaluationService.ExtractFields(code);

        Assert.Contains("TrackingNumber", fields);
        Assert.Contains("Weight", fields);
    }

    [Fact]
    public void Evaluate_ReportsMissedFields_FromReference()
    {
        using var temp = new TempDirs();
        File.WriteAllText(Path.Combine(temp.Reference, "Ref.cs"),
            "public string TrackingNumber { get; set; } public string LabelData { get; set; }");
        File.WriteAllText(Path.Combine(temp.Generated, "Gen.cs"),
            "public string TrackingNumber { get; set; }");

        var result = new EvaluationService().Evaluate(temp.Generated, temp.Reference);

        Assert.Contains("TrackingNumber", result.MatchedFields);
        Assert.Contains("LabelData", result.MissedFields);
    }

    private sealed class TempDirs : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "coa-eval-" + Guid.NewGuid().ToString("N"));
        public string Generated { get; }
        public string Reference { get; }

        public TempDirs()
        {
            Generated = Path.Combine(Root, "generated");
            Reference = Path.Combine(Root, "reference");
            Directory.CreateDirectory(Generated);
            Directory.CreateDirectory(Reference);
        }

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); } catch { /* best effort */ }
        }
    }
}
