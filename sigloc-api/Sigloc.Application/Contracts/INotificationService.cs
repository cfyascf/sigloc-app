using Sigloc.Application.DTOs;

namespace Sigloc.Application.Contracts;

/// <summary>Reads and acknowledges the notifications of the signed-in user's company.</summary>
public interface INotificationService
{
    Task<NotificationListDto> ListAsync(Guid companyId, int? limit, CancellationToken cancellationToken = default);

    Task<UnreadNotificationsDto> CountUnreadAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task MarkReadAsync(Guid companyId, Guid notificationId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(Guid companyId, CancellationToken cancellationToken = default);
}
