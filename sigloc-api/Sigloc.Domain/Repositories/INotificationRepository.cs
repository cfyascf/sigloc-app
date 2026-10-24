using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories;

public interface INotificationRepository
{
    /// <summary>
    /// Persists the notifications immediately, in a context of their own, so they never ride on
    /// (or roll back with) the business unit of work that triggered them.
    /// </summary>
    Task AddRangeAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken = default);

    /// <summary>The company's most recent notifications, newest first.</summary>
    Task<IReadOnlyList<Notification>> ListRecentAsync(Guid companyId, int take, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Marks one of the company's notifications as read; false when it does not belong to the company.</summary>
    Task<bool> MarkReadAsync(Guid companyId, Guid notificationId, DateTimeOffset readAt, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(Guid companyId, DateTimeOffset readAt, CancellationToken cancellationToken = default);
}
