namespace Sigloc.Application.Contracts;

/// <summary>A notification to create for one recipient company. Built by <c>NotificationCatalog</c>.</summary>
public record NotificationMessage(Guid RecipientCompanyId, string Type, string Title, string Message, string? Link = null);

/// <summary>
/// Records in-app notifications. Called after the business operation committed; a failure is
/// logged and swallowed, so a notification problem never fails (or reverts) the operation itself.
/// </summary>
public interface INotificationPublisher
{
    Task PublishAsync(IReadOnlyCollection<NotificationMessage> messages);
}

/// <summary>Publisher that drops every message; the default where no publisher is injected (tests).</summary>
public sealed class NullNotificationPublisher : INotificationPublisher
{
    public static readonly NullNotificationPublisher Instance = new();

    public Task PublishAsync(IReadOnlyCollection<NotificationMessage> messages) => Task.CompletedTask;
}
