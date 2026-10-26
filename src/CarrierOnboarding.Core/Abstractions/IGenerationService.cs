namespace CarrierOnboarding.Core.Abstractions;

/// <summary>Generates a DRAFT carrier integration from indexed docs and example code.</summary>
public interface IGenerationService
{
    Task<GenerationResult> GenerateAsync(
        string carrier,
        string examplesFolder,
        string outputFolder,
        CancellationToken cancellationToken = default);
}

public sealed class GenerationResult
{
    public required string Carrier { get; init; }
    public required string OutputFolder { get; init; }
    public required IReadOnlyList<string> GeneratedFiles { get; init; }
}
