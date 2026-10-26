namespace CarrierOnboarding.Core.Ingestion;

/// <summary>
/// Rough token estimation shared by chunking and prompt budgeting. We approximate
/// the common heuristic of ~4 characters per token used by OpenAI-family tokenizers.
/// </summary>
public static class TokenEstimator
{
    public static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // ~4 chars per token is a good cross-model approximation for English text.
        return (int)Math.Ceiling(text.Length / 4.0);
    }
}
