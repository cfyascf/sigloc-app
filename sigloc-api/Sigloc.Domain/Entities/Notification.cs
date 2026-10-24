using System.ComponentModel.DataAnnotations.Schema;

namespace Sigloc.Domain.Entities;

/// <summary>
/// In-app notification shown in the header bell of every user of the recipient company
/// (a contractor or a carrier). Created after the business operation it reports has committed.
/// </summary>
[Table("Notification")]
public class Notification : BaseEntity
{
    /// <summary>Contractor or carrier whose users see the notification.</summary>
    public Guid RecipientCompanyId { get; set; }

    /// <summary>Event code (e.g. "NOVO_LANCE"), used by the UI to pick an icon.</summary>
    public required string Type { get; set; }

    public required string Title { get; set; }

    public required string Message { get; set; }

    /// <summary>In-app path the notification opens (e.g. "/auction-bids/{id}"), when there is one.</summary>
    public string? Link { get; set; }

    /// <summary>When the company marked it as read; null while unread.</summary>
    public DateTimeOffset? ReadAt { get; set; }
}
