using Sigloc.Application.Contracts;

namespace Sigloc.Tests.Common;

/// <summary>Keeps every published message, for services that take an optional publisher.</summary>
public sealed class RecordingNotificationPublisher : INotificationPublisher
{
    public List<NotificationMessage> Sent { get; } = new();

    public Task PublishAsync(IReadOnlyCollection<NotificationMessage> messages)
    {
        Sent.AddRange(messages);
        return Task.CompletedTask;
    }
}
