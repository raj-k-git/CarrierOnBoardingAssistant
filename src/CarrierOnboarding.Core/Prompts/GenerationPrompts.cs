using System.Text;
using CarrierOnboarding.Core.Models;

namespace CarrierOnboarding.Core.Prompts;

/// <summary>Fixed retrieval topics and per-file instructions for the <c>generate</c> command.</summary>
public static class GenerationPrompts
{
    /// <summary>The fixed retrieval queries run before generation.</summary>
    public static readonly IReadOnlyDictionary<string, string> RetrievalTopics = new Dictionary<string, string>
    {
        ["authentication"] = "authentication, API keys, OAuth tokens, credentials, authorization headers",
        ["request_fields"] = "create label or shipment request fields, required parameters, address, parcel, weight, dimensions",
        ["response_format"] = "response format, label format, returned fields, tracking number, label image encoding",
        ["error_codes"] = "error codes, error responses, failure messages, status codes",
        ["rate_limits"] = "rate limits, throttling, requests per second, quotas",
        ["sandbox"] = "test and sandbox endpoints, base URLs, environments, staging",
    };

    public sealed record FileSpec(string FileName, string Instruction, IReadOnlyList<string> Topics);

    public static IReadOnlyList<FileSpec> FileSpecs(string carrier) => new List<FileSpec>
    {
        new($"{carrier}Client.cs",
            $"Generate the {carrier}Client class: the HTTP client that authenticates and calls the carrier " +
            "endpoints to create a shipment/label. Mirror the example client's constructor, dependency " +
            "injection, logging and exception-handling patterns.",
            new[] { "authentication", "request_fields", "response_format", "sandbox", "rate_limits" }),

        new($"{carrier}Models.cs",
            $"Generate the request and response model classes for the {carrier} create-label call, " +
            "following the example's model style (properties, naming, serialization attributes).",
            new[] { "request_fields", "response_format" }),

        new($"{carrier}FieldMapping.cs",
            $"Generate a {carrier}FieldMapping class that maps our canonical shipment model fields " +
            "(listed below) onto the carrier request fields. Add a // TODO: verify comment for any field " +
            "not clearly documented.",
            new[] { "request_fields" }),

        new($"{carrier}ErrorMapping.cs",
            $"Generate a {carrier}ErrorMapping class mapping documented carrier error codes to our " +
            "integration exceptions/messages, following the example's exception pattern.",
            new[] { "error_codes" }),

        new($"{carrier}ClientTests.cs",
            $"Generate xUnit test STUBS for {carrier}Client covering authentication, a successful " +
            "label request, and error-code handling. Use [Fact]/[Theory] with Arrange/Act/Assert and " +
            "// TODO: verify where doc detail is missing.",
            new[] { "authentication", "request_fields", "error_codes" }),
    };

    public static string BuildFileUserPrompt(
        string carrier,
        FileSpec spec,
        string exampleCode,
        IReadOnlyList<RetrievedChunk> chunks)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Carrier: {carrier}");
        sb.AppendLine($"Target file: {spec.FileName}");
        sb.AppendLine();
        sb.AppendLine("Task:");
        sb.AppendLine(spec.Instruction);
        sb.AppendLine();

        if (spec.FileName.Contains("FieldMapping", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("Our canonical shipment source fields:");
            foreach (var f in ShipmentModel.Fields)
            {
                sb.AppendLine($"  - {f}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("EXAMPLE integration code to follow EXACTLY for patterns:");
        sb.AppendLine("```csharp");
        sb.AppendLine(exampleCode.Trim());
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("Retrieved documentation excerpts (the ONLY source of carrier facts):");
        sb.AppendLine(PromptBuilder.RenderChunks(chunks));
        sb.AppendLine();
        sb.AppendLine("Output only the C# code for this one file. No markdown fences, no commentary.");
        return sb.ToString();
    }

    public const string MappingReportSystemPrompt =
        "You are a senior .NET integration engineer producing a field-mapping report for human review. " +
        "Use ONLY the retrieved documentation excerpts. Never invent carrier field names or values. " +
        "For anything the documentation does not clearly state, add it to an OPEN QUESTIONS list.";

    public static string BuildMappingReportUserPrompt(string carrier, IReadOnlyList<RetrievedChunk> chunks)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Produce MAPPING_REPORT.md for the {carrier} integration.");
        sb.AppendLine();
        sb.AppendLine("Write a Markdown document with:");
        sb.AppendLine("1. A table with columns: Carrier Field | Our Source Field | Doc Section | Notes.");
        sb.AppendLine("2. An 'OPEN QUESTIONS' section listing every field or behavior the docs were unclear about.");
        sb.AppendLine();
        sb.AppendLine("Our canonical shipment source fields:");
        foreach (var f in ShipmentModel.Fields)
        {
            sb.AppendLine($"  - {f}");
        }

        sb.AppendLine();
        sb.AppendLine("Retrieved documentation excerpts (the ONLY source of carrier facts):");
        sb.AppendLine(PromptBuilder.RenderChunks(chunks));
        return sb.ToString();
    }
}
