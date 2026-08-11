namespace Soraeru.Infrastructure.Llm;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>OpenAICompatible (default). Uses Chat Completions JSON.</summary>
    public string Provider { get; set; } = "OpenAICompatible";

    /// <summary>API key from User Secrets / env. Never commit real keys.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Low-cost text model. Examples: gemini-3.6-flash, gpt-4o-mini.
    /// </summary>
    public string Model { get; set; } = "gemini-3.6-flash";

    /// <summary>
    /// OpenAI-compatible base URL ending without trailing slash.
    /// Gemini AI Studio: https://generativelanguage.googleapis.com/v1beta/openai
    /// </summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/openai";

    public int TimeoutSeconds { get; set; } = 60;
}
