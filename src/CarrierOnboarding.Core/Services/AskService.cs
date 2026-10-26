using CarrierOnboarding.Core.Abstractions;
using CarrierOnboarding.Core.Prompts;
using Microsoft.Extensions.Logging;

namespace CarrierOnboarding.Core.Services;

/// <summary>Answers a question grounded strictly in the top-K retrieved chunks.</summary>
public sealed class AskService : IAskService
{
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _store;
    private readonly IChatService _chat;
    private readonly ILogger<AskService> _logger;

    public AskService(
        IEmbeddingService embeddings,
        IVectorStore store,
        IChatService chat,
        ILogger<AskService> logger)
    {
        _embeddings = embeddings;
        _store = store;
        _chat = chat;
        _logger = logger;
    }

    public async Task<AskResult> AskAsync(
        string carrier,
        string question,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddings.EmbedAsync(question, cancellationToken);
        var sources = await _store.SearchAsync(carrier, queryEmbedding, topK, cancellationToken);

        if (sources.Count == 0)
        {
            _logger.LogWarning("No indexed content found for carrier {Carrier}", carrier);
            return new AskResult { Answer = "not found in document", Sources = sources };
        }

        var userPrompt = PromptBuilder.BuildAskUserPrompt(question, sources);
        var answer = await _chat.CompleteAsync(PromptBuilder.AskSystemPrompt, userPrompt, cancellationToken);

        return new AskResult { Answer = answer.Trim(), Sources = sources };
    }
}
