using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soraeru.Application.Abstractions.Llm;

namespace Soraeru.Infrastructure.Llm;

/// <summary>
/// OpenAI-compatible Chat Completions client (works with Google AI Studio OpenAI endpoint too).
/// </summary>
public sealed class OpenAiCompatibleWordAnalysisAgent : IWordAnalysisAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly LlmOptions _options;
    private readonly ILogger<OpenAiCompatibleWordAnalysisAgent> _logger;

    public OpenAiCompatibleWordAnalysisAgent(
        HttpClient http,
        IOptions<LlmOptions> options,
        ILogger<OpenAiCompatibleWordAnalysisAgent> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WordAnalysisAgentOutcome> AnalyzeAsync(
        WordAnalysisAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var systemPrompt = request.SkipMnemonics
            ? WordAnalysisPrompts.MeaningReadingOnlySystem
            : WordAnalysisPrompts.System;
        var userPrompt = request.SkipMnemonics
            ? WordAnalysisPrompts.BuildMeaningReadingUserPrompt(
                request.Text,
                request.SourceLanguage,
                request.MemoryLanguage)
            : WordAnalysisPrompts.BuildUserPrompt(
                request.Text,
                request.SourceLanguage,
                request.MemoryLanguage,
                request.NotationPreference);

        var body = new ChatCompletionRequest(
            _options.Model,
            [
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user", userPrompt)
            ],
            Temperature: 0.4,
            ResponseFormat: new ResponseFormat("json_object"));

        using var response = await _http.PostAsJsonAsync("chat/completions", body, JsonOptions, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("LLM HTTP {Status}: {Body}", (int)response.StatusCode, Truncate(raw));
            return new WordAnalysisAgentFailure(
                "LLM_HTTP_ERROR",
                $"LLM 呼叫失敗（{(int)response.StatusCode}）。");
        }

        ChatCompletionResponse? completion;
        try
        {
            completion = JsonSerializer.Deserialize<ChatCompletionResponse>(raw, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse chat completion envelope.");
            return new WordAnalysisAgentFailure("LLM_PARSE_ERROR", "無法解析 LLM 回應。");
        }

        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return new WordAnalysisAgentFailure("LLM_EMPTY", "LLM 未回傳內容。");
        }

        var json = UnwrapMarkdownFence(content.Trim());
        return ParsePayload(json);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey)
            || _options.ApiKey.Contains("REPLACE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "LLM API Key 尚未設定。請用 User Secrets 設定 Llm:ApiKey（見 docs/dev-setup-llm.md）。");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException("Llm:Model 尚未設定。");
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("Llm:BaseUrl 尚未設定。");
        }
    }

    private static WordAnalysisAgentOutcome ParsePayload(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var errorEl))
        {
            var code = errorEl.ValueKind == JsonValueKind.String
                ? errorEl.GetString() ?? "UNANALYZABLE"
                : "UNANALYZABLE";
            var message = root.TryGetProperty("message", out var msgEl)
                ? msgEl.GetString() ?? "無法分析此文字。"
                : "無法分析此文字。";
            return new WordAnalysisAgentFailure(code, message);
        }

        var payload = JsonSerializer.Deserialize<LlmJsonPayload>(json, JsonOptions);
        if (payload is null)
        {
            return new WordAnalysisAgentFailure("LLM_PARSE_ERROR", "無法反序列化分析 JSON。");
        }

        var mnemonics = (payload.Mnemonics ?? [])
            .Select(m => new WordAnalysisMnemonic(
                m.DisplayText ?? "",
                m.NotationType ?? "",
                m.NotationText ?? "",
                m.Explanation ?? ""))
            .ToList();

        return new WordAnalysisAgentSuccess(
            new WordAnalysisPayload(
                payload.SourceText ?? "",
                payload.NormalizedText ?? "",
                payload.SourceLanguage ?? "",
                payload.LanguageDisplayName ?? "",
                payload.Meaning ?? "",
                payload.ReadingText ?? "",
                mnemonics,
                payload.Notice ?? ""));
    }

    private static string UnwrapMarkdownFence(string content)
    {
        if (!content.StartsWith("```", StringComparison.Ordinal))
            return content;

        var lines = content.Split('\n');
        var sb = new StringBuilder();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                break;
            sb.AppendLine(line);
        }

        return sb.ToString().Trim();
    }

    private static string Truncate(string value) =>
        value.Length <= 500 ? value : value[..500] + "…";

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("response_format")] ResponseFormat ResponseFormat);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ResponseFormat(
        [property: JsonPropertyName("type")] string Type);

    private sealed class ChatCompletionResponse
    {
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        public ChatMessageDto? Message { get; set; }
    }

    private sealed class ChatMessageDto
    {
        public string? Content { get; set; }
    }

    private sealed class LlmJsonPayload
    {
        public string? SourceText { get; set; }
        public string? NormalizedText { get; set; }
        public string? SourceLanguage { get; set; }
        public string? LanguageDisplayName { get; set; }
        public string? Meaning { get; set; }
        public string? ReadingText { get; set; }
        public List<LlmMnemonic>? Mnemonics { get; set; }
        public string? Notice { get; set; }
    }

    private sealed class LlmMnemonic
    {
        public string? DisplayText { get; set; }
        public string? NotationType { get; set; }
        public string? NotationText { get; set; }
        public string? Explanation { get; set; }
    }
}

public static class OpenAiCompatibleWordAnalysisAgentExtensions
{
    public static void ConfigureHttpClient(HttpClient client, LlmOptions options)
    {
        var baseUrl = options.BaseUrl.TrimEnd('/') + "/";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 10, 180));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.ApiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
}
