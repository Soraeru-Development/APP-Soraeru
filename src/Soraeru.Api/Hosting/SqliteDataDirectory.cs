namespace Soraeru.Api.Hosting;

internal static class SqliteDataDirectory
{
    public static void EnsureExists(string? connectionString)
    {
        var dataSource = TryGetDataSource(connectionString);
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            return;
        }

        var directory = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    internal static string? TryGetDataSource(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = part[..separator].Trim();
            if (key.Equals("Data Source", StringComparison.OrdinalIgnoreCase)
                || key.Equals("DataSource", StringComparison.OrdinalIgnoreCase))
            {
                return part[(separator + 1)..].Trim().Trim('"');
            }
        }

        return null;
    }
}
