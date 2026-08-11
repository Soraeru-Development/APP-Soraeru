using System.Text.Json;

namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// JSON file-backed local word-card store (App SoT persistence across restarts).
/// </summary>
public sealed class JsonFileLocalWordCardStore : ILocalWordCardStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonFileLocalWordCardStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    public async Task<IReadOnlyList<LocalWordCard>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_path))
                return [];

            await using var stream = File.OpenRead(_path);
            var cards = await JsonSerializer.DeserializeAsync<List<LocalWordCard>>(
                stream,
                JsonOptions,
                cancellationToken);
            return cards ?? [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAllAsync(IReadOnlyList<LocalWordCard> cards, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cards);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var tempPath = _path + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, cards, JsonOptions, cancellationToken);
            }

            File.Move(tempPath, _path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }
}
