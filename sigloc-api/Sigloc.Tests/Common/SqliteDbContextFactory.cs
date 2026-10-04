using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Tests.Common;

/// <summary>
/// Creates a <see cref="SiglocDbContext"/> backed by a private SQLite in-memory
/// database. Each instance owns one open connection; the database lives as long as
/// that connection stays open, so the wrapper is disposable and must be disposed at
/// the end of a test.
///
/// SQLite gives us a real relational engine (foreign keys, unique indexes,
/// transactions) with zero external dependencies, so repository and DbContext tests
/// run fully offline — no Docker and no NeonDB/VPN access required.
/// </summary>
public sealed class SqliteDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SiglocDbContext> _options;

    public SqliteDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<SiglocDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new SqliteTestDbContext(_options);
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Returns a fresh context over the same underlying database. Using a new
    /// context per logical operation mirrors the scoped lifetime in production and
    /// avoids accidental first-level-cache hits hiding persistence bugs.
    /// </summary>
    public SiglocDbContext CreateContext() => new SqliteTestDbContext(_options);

    public void Dispose() => _connection.Dispose();
}
