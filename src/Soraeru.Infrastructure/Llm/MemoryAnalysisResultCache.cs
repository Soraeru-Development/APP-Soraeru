using System.Collections.Concurrent;
using Soraeru.Application.Abstractions.Llm;

namespace Soraeru.Infrastructure.Llm;

public sealed class MemoryAnalysisResultCache : IAnalysisResultCache
{
    private readonly ConcurrentDictionary<string, WordAnalysisPayload> _store = new(StringComparer.Ordinal);

    public bool TryGet(string cacheKey, out WordAnalysisPayload? payload) =>
        _store.TryGetValue(cacheKey, out payload);

    public void Set(string cacheKey, WordAnalysisPayload payload) =>
        _store[cacheKey] = payload;
}
