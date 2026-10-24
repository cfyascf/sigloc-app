using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public sealed class NotificationPublisher(INotificationRepository repository, ILogger<NotificationPublisher>? logger = null)
    : INotificationPublisher
{
    private const int MaxTitleLength = 120;
    private const int MaxMessageLength = 500;
    private readonly ILogger _logger = logger ?? NullLogger<NotificationPublisher>.Instance;

    public async Task PublishAsync(IReadOnlyCollection<NotificationMessage> messages)
    {
        if (messages.Count == 0) return;
        var notifications = messages.Select(m => new Notification
        {
            Id = Guid.NewGuid(),
            RecipientCompanyId = m.RecipientCompanyId,
            Type = m.Type,
            Title = Truncate(m.Title, MaxTitleLength),
            Message = Truncate(m.Message, MaxMessageLength),
            Link = m.Link
        }).ToList();
        try
        {
            // Not cancellable: the operation being reported already committed, so a client that
            // disconnected now must not leave its counterpart without the notification.
            await repository.AddRangeAsync(notifications, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not record {Count} notification(s) of type {Types}.",
                notifications.Count, string.Join(",", notifications.Select(n => n.Type).Distinct()));
        }
    }

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
}
