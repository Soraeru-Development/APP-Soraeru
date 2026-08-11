using Soraeru.Application.Abstractions.Llm;

namespace Soraeru.Infrastructure.Llm;

/// <summary>
/// Thrown path when Llm section exists but agent was not registered (should not be used after DI wiring).
/// Kept as explicit failure for misconfiguration debugging.
/// </summary>
public sealed class NotConfiguredWordAnalysisAgent : IWordAnalysisAgent
{
    public Task<WordAnalysisAgentOutcome> AnalyzeAsync(
        WordAnalysisAgentRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<WordAnalysisAgentOutcome>(
            new WordAnalysisAgentFailure(
                "LLM_NOT_CONFIGURED",
                "Word Analysis Agent is not configured. Set Llm:ApiKey via User Secrets."));
}
