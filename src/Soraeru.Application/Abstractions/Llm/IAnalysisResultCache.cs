namespace Soraeru.Application.Abstractions.Llm;

/// <summary>
/// Process-level cache for successful analyze payloads (language + normalized + notation + prompt ver).
/// </summary>
public interface IAnalysisResultCache
{
    bool TryGet(string cacheKey, out WordAnalysisPayload? payload);

    void Set(string cacheKey, WordAnalysisPayload payload);
}
