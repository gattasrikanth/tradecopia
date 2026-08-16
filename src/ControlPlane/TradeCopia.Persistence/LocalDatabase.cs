using Microsoft.Data.Sqlite;

namespace TradeCopia.Persistence;

public sealed class LocalDatabase : IDisposable
{
    private readonly string _path;
    private readonly SqliteConnection _connection;

    public LocalDatabase(string directory, string fileName)
    {
        Directory.CreateDirectory(directory);
        _path = System.IO.Path.Combine(directory, fileName);
        _connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString());
        _connection.Open();
        using var pragma = _connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        pragma.ExecuteNonQuery();
        Migrate();
    }

    public string DatabasePath => _path;

    public SqliteConnection Connection => _connection;

    public void Dispose()
    {
        _connection.Dispose();
    }

    private void Migrate()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS schema_info (
              version INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS config_drafts (
              id TEXT PRIMARY KEY,
              name TEXT NOT NULL,
              payload_json TEXT NOT NULL,
              updated_at_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS journal_trades (
              id TEXT PRIMARY KEY,
              group_name TEXT NOT NULL,
              instrument TEXT NOT NULL,
              side TEXT NOT NULL,
              opened_at_utc TEXT NOT NULL,
              payload_json TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS settings (
              key TEXT PRIMARY KEY,
              value TEXT NOT NULL
            );
            INSERT INTO schema_info (version)
            SELECT 1 WHERE NOT EXISTS (SELECT 1 FROM schema_info);
            """;
        cmd.ExecuteNonQuery();
    }
}
