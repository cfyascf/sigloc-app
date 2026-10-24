namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised when a notification does not exist or belongs to another company. Produces a 404.
/// </summary>
public sealed class NotificationNotFoundException : Exception
{
    public NotificationNotFoundException(Guid notificationId)
        : base($"Notificação '{notificationId}' não encontrada.")
    {
    }
}
