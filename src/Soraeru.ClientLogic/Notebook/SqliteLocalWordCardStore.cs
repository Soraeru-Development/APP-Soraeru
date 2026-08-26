using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// SQLite-backed local word-card store (App SoT). Rows coexist for multiple OwnerUserId values.
/// Optionally migrates a one-time legacy JSON file into the DB.
/// </summary>
public sealed class SqliteLocalWordCardStore : ILocalWordCardStore
{
    private static readonly JsonSerializerOptions LegacyJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _dbPath;
    private readonly string? _legacyJsonPath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public SqliteLocalWordCardStore(string dbPath, string? legacyJsonPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbPath);
        _dbPath = dbPath;
        _legacyJsonPath = string.IsNullOrWhiteSpace(legacyJsonPath) ? null : legacyJsonPath;
        SQLitePCL.Batteries_V2.Init();
    }

    public async Task<IReadOnlyList<LocalWordCard>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureInitializedAsync(cancellationToken);
            await using var connection = OpenConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT Id, OwnerUserId, SourceText, NormalizedText, DetectedLanguage,
                       MeaningZh, Pronunciation, SelectedMnemonic,
                       CreatedAtUtc, UpdatedAtUtc, DeletedAtUtc
                FROM WordCards
                ORDER BY UpdatedAtUtc DESC;
                """;

            var cards = new List<LocalWordCard>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                cards.Add(ReadCard(reader));
            }

            return cards;
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
            await EnsureInitializedAsync(cancellationToken);
            await using var connection = OpenConnection();
            await connection.OpenAsync(cancellationToken);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

            await using (var delete = connection.CreateCommand())
            {
                delete.Transaction = tx;
                delete.CommandText = "DELETE FROM WordCards;";
                await delete.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var card in cards)
            {
                await InsertCardAsync(connection, tx, card, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        var directory = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using (var create = connection.CreateCommand())
        {
            create.CommandText =
                """
                CREATE TABLE IF NOT EXISTS WordCards (
                    Id TEXT NOT NULL PRIMARY KEY,
                    OwnerUserId TEXT NOT NULL,
                    SourceText TEXT NOT NULL,
                    NormalizedText TEXT NOT NULL,
                    DetectedLanguage TEXT NOT NULL,
                    MeaningZh TEXT NOT NULL,
                    Pronunciation TEXT NOT NULL,
                    SelectedMnemonic TEXT NOT NULL,
                    CreatedAtUtc TEXT NOT NULL,
                    UpdatedAtUtc TEXT NOT NULL,
                    DeletedAtUtc TEXT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_WordCards_OwnerUserId ON WordCards(OwnerUserId);
                """;
            await create.ExecuteNonQueryAsync(cancellationToken);
        }

        await TryMigrateLegacyJsonAsync(connection, cancellationToken);
        _initialized = true;
    }

    private async Task TryMigrateLegacyJsonAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        if (_legacyJsonPath is null || !File.Exists(_legacyJsonPath))
            return;

        List<LocalWordCard>? legacy;
        await using (var stream = File.OpenRead(_legacyJsonPath))
        {
            legacy = await JsonSerializer.DeserializeAsync<List<LocalWordCard>>(
                stream,
                LegacyJsonOptions,
                cancellationToken);
        }

        if (legacy is { Count: > 0 })
        {
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            foreach (var card in legacy)
            {
                await using var upsert = connection.CreateCommand();
                upsert.Transaction = tx;
                upsert.CommandText =
                    """
                    INSERT INTO WordCards (
                        Id, OwnerUserId, SourceText, NormalizedText, DetectedLanguage,
                        MeaningZh, Pronunciation, SelectedMnemonic,
                        CreatedAtUtc, UpdatedAtUtc, DeletedAtUtc)
                    VALUES (
                        $id, $owner, $source, $normalized, $lang,
                        $meaning, $pron, $mnemonic,
                        $created, $updated, $deleted)
                    ON CONFLICT(Id) DO UPDATE SET
                        OwnerUserId = excluded.OwnerUserId,
                        SourceText = excluded.SourceText,
                        NormalizedText = excluded.NormalizedText,
                        DetectedLanguage = excluded.DetectedLanguage,
                        MeaningZh = excluded.MeaningZh,
                        Pronunciation = excluded.Pronunciation,
                        SelectedMnemonic = excluded.SelectedMnemonic,
                        CreatedAtUtc = excluded.CreatedAtUtc,
                        UpdatedAtUtc = excluded.UpdatedAtUtc,
                        DeletedAtUtc = excluded.DeletedAtUtc;
                    """;
                BindCard(upsert, card);
                await upsert.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
        }

        var migratedPath = _legacyJsonPath + ".migrated";
        if (File.Exists(migratedPath))
            File.Delete(migratedPath);
        File.Move(_legacyJsonPath, migratedPath);
    }

    private SqliteConnection OpenConnection() =>
        new(new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString());

    private static async Task InsertCardAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        LocalWordCard card,
        CancellationToken cancellationToken)
    {
        await using var insert = connection.CreateCommand();
        insert.Transaction = tx;
        insert.CommandText =
            """
            INSERT INTO WordCards (
                Id, OwnerUserId, SourceText, NormalizedText, DetectedLanguage,
                MeaningZh, Pronunciation, SelectedMnemonic,
                CreatedAtUtc, UpdatedAtUtc, DeletedAtUtc)
            VALUES (
                $id, $owner, $source, $normalized, $lang,
                $meaning, $pron, $mnemonic,
                $created, $updated, $deleted);
            """;
        BindCard(insert, card);
        await insert.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void BindCard(SqliteCommand command, LocalWordCard card)
    {
        command.Parameters.AddWithValue("$id", card.Id.ToString("D"));
        command.Parameters.AddWithValue("$owner", card.OwnerUserId.ToString("D"));
        command.Parameters.AddWithValue("$source", card.SourceText);
        command.Parameters.AddWithValue("$normalized", card.NormalizedText);
        command.Parameters.AddWithValue("$lang", card.DetectedLanguage);
        command.Parameters.AddWithValue("$meaning", card.MeaningZh);
        command.Parameters.AddWithValue("$pron", card.Pronunciation);
        command.Parameters.AddWithValue("$mnemonic", card.SelectedMnemonic);
        command.Parameters.AddWithValue("$created", FormatOffset(card.CreatedAtUtc));
        command.Parameters.AddWithValue("$updated", FormatOffset(card.UpdatedAtUtc));
        command.Parameters.AddWithValue(
            "$deleted",
            card.DeletedAtUtc is { } deleted ? FormatOffset(deleted) : DBNull.Value);
    }

    private static LocalWordCard ReadCard(SqliteDataReader reader) =>
        new(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            ParseOffset(reader.GetString(8)),
            ParseOffset(reader.GetString(9)),
            reader.IsDBNull(10) ? null : ParseOffset(reader.GetString(10)));

    private static string FormatOffset(DateTimeOffset value) =>
        value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseOffset(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
