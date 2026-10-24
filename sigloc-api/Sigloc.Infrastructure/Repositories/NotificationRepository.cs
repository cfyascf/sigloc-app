using Microsoft.EntityFrameworkCore;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;
using Sigloc.Infrastructure.Contexts;

namespace Sigloc.Infrastructure.Repositories;

/// <summary>
/// Every call uses a short-lived context from the factory: notifications are written after the
/// triggering operation committed, and must not pick up (or save) that operation's tracked changes.
/// </summary>
public class NotificationRepository : INotificationRepository
{
    private readonly IDbContextFactory<SiglocDbContext> _contextFactory;

    public NotificationRepository(IDbContextFactory<SiglocDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddRangeAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken = default)
    {
        if (notifications.Count == 0)
        {
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Notifications.AddRangeAsync(notifications, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> ListRecentAsync(Guid companyId, int take, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientCompanyId == companyId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountUnreadAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Notifications
            .CountAsync(n => n.RecipientCompanyId == companyId && n.ReadAt == null, cancellationToken);
    }

    public async Task<bool> MarkReadAsync(Guid companyId, Guid notificationId, DateTimeOffset readAt, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientCompanyId == companyId, cancellationToken);
        if (notification is null)
        {
            return false;
        }

        notification.ReadAt ??= readAt;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkAllReadAsync(Guid companyId, DateTimeOffset readAt, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Notifications
            .Where(n => n.RecipientCompanyId == companyId && n.ReadAt == null)
            .ExecuteUpdateAsync(set => set
                .SetProperty(n => n.ReadAt, readAt.ToUniversalTime())
                .SetProperty(n => n.UpdatedAt, readAt.ToUniversalTime()), cancellationToken);
    }
}
