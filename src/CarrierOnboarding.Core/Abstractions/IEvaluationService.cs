namespace CarrierOnboarding.Core.Abstractions;

/// <summary>
/// Compares a generated draft integration against an existing hand-written one and
/// reports which fields and error codes were matched or missed. Heuristic / static only.
/// </summary>
public interface IEvaluationService
{
    EvaluationResult Evaluate(string generatedFolder, string referenceFolder);
}

public sealed class EvaluationResult
{
    public required IReadOnlyList<string> MatchedFields { get; init; }
    public required IReadOnlyList<string> MissedFields { get; init; }
    public required IReadOnlyList<string> MatchedErrorCodes { get; init; }
    public required IReadOnlyList<string> MissedErrorCodes { get; init; }

    public double FieldCoverage =>
        MatchedFields.Count + MissedFields.Count == 0
            ? 1d
            : (double)MatchedFields.Count / (MatchedFields.Count + MissedFields.Count);

    public double ErrorCodeCoverage =>
        MatchedErrorCodes.Count + MissedErrorCodes.Count == 0
            ? 1d
            : (double)MatchedErrorCodes.Count / (MatchedErrorCodes.Count + MissedErrorCodes.Count);
}
