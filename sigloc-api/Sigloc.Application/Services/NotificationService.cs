using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public sealed class NotificationService(INotificationRepository repository, TimeProvider clock) : INotificationService
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    public async Task<NotificationListDto> ListAsync(Guid companyId, int? limit, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
        var items = await repository.ListRecentAsync(companyId, take, cancellationToken);
        var unread = await repository.CountUnreadAsync(companyId, cancellationToken);
        return new NotificationListDto(
            items.Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Message, n.Link, n.ReadAt != null, n.CreatedAt)).ToList(),
            unread);
    }

    public async Task<UnreadNotificationsDto> CountUnreadAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        new(await repository.CountUnreadAsync(companyId, cancellationToken));

    public async Task MarkReadAsync(Guid companyId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        if (!await repository.MarkReadAsync(companyId, notificationId, clock.GetUtcNow(), cancellationToken))
            throw new NotificationNotFoundException(notificationId);
    }

    public Task MarkAllReadAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        repository.MarkAllReadAsync(companyId, clock.GetUtcNow(), cancellationToken);
}
