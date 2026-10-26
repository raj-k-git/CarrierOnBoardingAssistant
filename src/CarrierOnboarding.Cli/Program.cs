using System.CommandLine;
using CarrierOnboarding.Cli;
using CarrierOnboarding.Core.Abstractions;
using CarrierOnboarding.Core.Configuration;
using CarrierOnboarding.Core.DependencyInjection;
using CarrierOnboarding.Core.Ingestion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets(typeof(Program).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var llmOptions = configuration.GetSection(LlmOptions.SectionName).Get<LlmOptions>() ?? new LlmOptions();
var chunkingOptions = configuration.GetSection("Chunking").Get<ChunkingOptions>() ?? new ChunkingOptions();

// Default on-disk location so `index` and a later `ask`/`generate` share the vector data.
var vectorStorePath = Path.Combine(Directory.GetCurrentDirectory(), ".carrier-index", "index.json");

// Build the service provider lazily, since `evaluate` needs no LLM provider at all.
ServiceProvider BuildProvider()
{
    var services = new ServiceCollection();
    services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Information));

    var (chat, embeddings) = KernelFactory.Create(llmOptions);
    services.AddSingleton(chat);
    services.AddSingleton(embeddings);

    services.AddCarrierOnboardingCore(chunkingOptions, vectorStorePath);
    return services.BuildServiceProvider();
}

ServiceProvider BuildProviderNoLlm()
{
    var services = new ServiceCollection();
    services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Information));
    services.AddSingleton<IEvaluationService, CarrierOnboarding.Core.Services.EvaluationService>();
    return services.BuildServiceProvider();
}

var carrierOption = new Option<string>("--carrier", "The carrier name used to tag indexed content.") { IsRequired = true };

// ----- index -----
var docOption = new Option<FileInfo>("--doc", "Path to the carrier API integration guide (pdf/docx/txt/md).") { IsRequired = true };
var indexCommand = new Command("index", "Extract, chunk, embed and store a carrier document.")
{
    docOption, carrierOption,
};
indexCommand.SetHandler(async (FileInfo doc, string carrier) =>
{
    await using var provider = BuildProvider();
    var service = provider.GetRequiredService<IIndexingService>();
    var result = await service.IndexAsync(doc.FullName, carrier);
    Console.WriteLine($"Indexed {result.ChunkCount} chunk(s) from '{result.Document}' for carrier '{result.Carrier}'.");
}, docOption, carrierOption);

// ----- ask -----
var questionOption = new Option<string>("--question", "The question to answer from the indexed document.") { IsRequired = true };
var askCommand = new Command("ask", "Answer a question using only the indexed carrier document.")
{
    carrierOption, questionOption,
};
askCommand.SetHandler(async (string carrier, string question) =>
{
    await using var provider = BuildProvider();
    var service = provider.GetRequiredService<IAskService>();
    var result = await service.AskAsync(carrier, question);

    Console.WriteLine();
    Console.WriteLine(result.Answer);
    Console.WriteLine();
    Console.WriteLine("Sources:");
    foreach (var s in result.Sources)
    {
        Console.WriteLine($"  - {s.Chunk.Citation} (score {s.Score:0.00})");
    }
}, carrierOption, questionOption);

// ----- generate -----
var examplesOption = new Option<DirectoryInfo>("--examples", "Folder with 1-2 existing carrier integrations to use as pattern examples.") { IsRequired = true };
var outOption = new Option<DirectoryInfo>("--out", "Output folder for the DRAFT generated files.") { IsRequired = true };
var generateCommand = new Command("generate", "Generate a DRAFT carrier integration for engineer review.")
{
    carrierOption, examplesOption, outOption,
};
generateCommand.SetHandler(async (string carrier, DirectoryInfo examples, DirectoryInfo output) =>
{
    await using var provider = BuildProvider();
    var service = provider.GetRequiredService<IGenerationService>();
    var result = await service.GenerateAsync(carrier, examples.FullName, output.FullName);

    Console.WriteLine($"Generated {result.GeneratedFiles.Count} DRAFT file(s) in '{result.OutputFolder}':");
    foreach (var f in result.GeneratedFiles)
    {
        Console.WriteLine($"  - {Path.GetFileName(f)}");
    }

    Console.WriteLine();
    Console.WriteLine("These are DRAFTS only. Review MAPPING_REPORT.md and every // TODO: verify before use.");
}, carrierOption, examplesOption, outOption);

// ----- evaluate -----
var generatedOption = new Option<DirectoryInfo>("--generated", "Folder with the generated draft to evaluate.") { IsRequired = true };
var referenceOption = new Option<DirectoryInfo>("--reference", "Folder with the hand-written reference integration.") { IsRequired = true };
var evaluateCommand = new Command("evaluate", "Compare a generated draft against a reference integration.")
{
    carrierOption, generatedOption, referenceOption,
};
evaluateCommand.SetHandler((string carrier, DirectoryInfo generated, DirectoryInfo reference) =>
{
    using var provider = BuildProviderNoLlm();
    var service = provider.GetRequiredService<IEvaluationService>();
    var result = service.Evaluate(generated.FullName, reference.FullName);

    Console.WriteLine($"Evaluation for carrier '{carrier}':");
    Console.WriteLine($"  Field coverage:      {result.FieldCoverage:P0} ({result.MatchedFields.Count} matched, {result.MissedFields.Count} missed)");
    Console.WriteLine($"  Error-code coverage: {result.ErrorCodeCoverage:P0} ({result.MatchedErrorCodes.Count} matched, {result.MissedErrorCodes.Count} missed)");
    Console.WriteLine();
    PrintList("Missed fields", result.MissedFields);
    PrintList("Missed error codes", result.MissedErrorCodes);
}, carrierOption, generatedOption, referenceOption);

var rootCommand = new RootCommand(
    "Carrier Onboarding Assistant - RAG tool that drafts C# carrier integrations for review. Never deploys.")
{
    indexCommand, askCommand, generateCommand, evaluateCommand,
};

return await rootCommand.InvokeAsync(args);

static void PrintList(string title, IReadOnlyList<string> items)
{
    Console.WriteLine($"{title}: {(items.Count == 0 ? "none" : string.Empty)}");
    foreach (var item in items)
    {
        Console.WriteLine($"  - {item}");
    }
}

/// <summary>Partial Program class so user-secrets can reference the assembly.</summary>
public partial class Program;
