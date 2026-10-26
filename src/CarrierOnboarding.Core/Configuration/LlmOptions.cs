namespace CarrierOnboarding.Core.Configuration;

/// <summary>
/// Strongly-typed configuration bound from the "Llm" section of appsettings.json.
/// No secrets are stored here in source; API keys come from user-secrets / env vars.
/// </summary>
public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>"AzureOpenAI" or "Ollama".</summary>
    public string Provider { get; set; } = "Ollama";

    public AzureOpenAiOptions AzureOpenAI { get; set; } = new();

    public OllamaOptions Ollama { get; set; } = new();
}

public sealed class AzureOpenAiOptions
{
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Deployment name of the chat completion model.</summary>
    public string ChatDeployment { get; set; } = string.Empty;

    /// <summary>Deployment name of the text embedding model.</summary>
    public string EmbeddingDeployment { get; set; } = string.Empty;

    /// <summary>
    /// API key. Should be provided via user-secrets or the AZURE_OPENAI_API_KEY
    /// environment variable, never committed to source control.
    /// </summary>
    public string? ApiKey { get; set; }
}

public sealed class OllamaOptions
{
    public string Endpoint { get; set; } = "http://localhost:11434";

    public string ChatModel { get; set; } = "llama3.1";

    public string EmbeddingModel { get; set; } = "nomic-embed-text";
}
