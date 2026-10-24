using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Infrastructure.Contexts;
using Sigloc.Infrastructure.Repositories;
using Sigloc.Tests.Common;

namespace Sigloc.Tests.Integration;

public class NotificationRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _otherCompanyId = Guid.NewGuid();

    public void Dispose() => _db.Dispose();

    private NotificationRepository Repo() => new(new Factory(_db));

    private sealed class Factory(SqliteDatabase db) : IDbContextFactory<SiglocDbContext>
    {
        public SiglocDbContext CreateDbContext() => db.CreateContext();
    }

    private static Notification Notification(Guid recipient, string title) => new()
    {
        Id = Guid.NewGuid(), RecipientCompanyId = recipient, Type = "NOVO_LANCE", Title = title, Message = "m", Link = "/x"
    };

    [Fact]
    public async Task Lists_only_the_companys_notifications_newest_first_and_counts_unread()
    {
        var repo = Repo();
        await repo.AddRangeAsync([Notification(_companyId, "first")]);
        await Task.Delay(5);
        await repo.AddRangeAsync([Notification(_companyId, "second"), Notification(_otherCompanyId, "other")]);

        var list = await repo.ListRecentAsync(_companyId, 10);

        list.Select(n => n.Title).Should().Equal("second", "first");
        list.Should().OnlyContain(n => n.CreatedAt != default);
        (await repo.CountUnreadAsync(_companyId)).Should().Be(2);
        (await repo.ListRecentAsync(_companyId, 1)).Should().ContainSingle().Which.Title.Should().Be("second");
    }

    [Fact]
    public async Task Marks_one_or_all_as_read_within_the_company_only()
    {
        var repo = Repo();
        var mine = Notification(_companyId, "mine");
        var theirs = Notification(_otherCompanyId, "theirs");
        await repo.AddRangeAsync([mine, Notification(_companyId, "also mine"), theirs]);
        var now = DateTimeOffset.UtcNow;

        (await repo.MarkReadAsync(_companyId, theirs.Id, now)).Should().BeFalse();
        (await repo.MarkReadAsync(_companyId, mine.Id, now)).Should().BeTrue();
        (await repo.CountUnreadAsync(_companyId)).Should().Be(1);

        await repo.MarkAllReadAsync(_companyId, now);

        (await repo.CountUnreadAsync(_companyId)).Should().Be(0);
        (await repo.CountUnreadAsync(_otherCompanyId)).Should().Be(1);
        (await repo.ListRecentAsync(_companyId, 10)).Should().OnlyContain(n => n.ReadAt != null);
    }
}
