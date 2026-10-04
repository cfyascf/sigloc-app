using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Tests.Common;

/// <summary>
/// Test-only <see cref="SiglocDbContext"/> that stores every <see cref="DateTimeOffset"/>
/// as UTC ticks (a <see cref="long"/>) when running on SQLite.
///
/// The production model applies a global DateTimeOffset-to-DateTimeOffset UTC converter,
/// which SQLite's provider cannot translate inside <c>ORDER BY</c>, <c>WHERE &gt;</c>, or
/// <c>Min</c>/<c>Max</c>. Ticks are monotonic integers, so all of those translate
/// correctly — giving the repository queries the same ordering/comparison semantics they
/// have on PostgreSQL. This affects only the in-memory test database; production is
/// untouched.
/// </summary>
public sealed class SqliteTestDbContext : SiglocDbContext
{
    public SqliteTestDbContext(DbContextOptions<SiglocDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var ticksConverter = new ValueConverter<DateTimeOffset, long>(
            v => v.ToUniversalTime().Ticks,
            v => new DateTimeOffset(v, TimeSpan.Zero));

        var nullableTicksConverter = new ValueConverter<DateTimeOffset?, long?>(
            v => v.HasValue ? v.Value.ToUniversalTime().Ticks : null,
            v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(ticksConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(nullableTicksConverter);
                }
            }
        }
    }
}
