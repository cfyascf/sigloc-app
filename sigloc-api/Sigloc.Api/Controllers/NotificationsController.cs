using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Application.Contracts;
using Sigloc.Api.Extensions;

namespace Sigloc.Api.Controllers;

/// <summary>Notifications of the signed-in user's company (contractor or carrier), shown in the header bell.</summary>
[ApiController]
[Route("api/notificacoes")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    /// <summary>GET /api/notificacoes?limite=20 - the newest notifications (max 50) and the unread count.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery(Name = "limite")] int? limit, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await _notifications.ListAsync(User.GetCompanyId(), limit, cancellationToken));
    }

    /// <summary>GET /api/notificacoes/nao-lidas - unread count only, for the bell badge polling.</summary>
    [HttpGet("nao-lidas")]
    public async Task<IActionResult> CountUnread(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await _notifications.CountUnreadAsync(User.GetCompanyId(), cancellationToken));
    }

    /// <summary>POST /api/notificacoes/{id}/lida - marks one notification as read (404 when it is not the company's).</summary>
    [HttpPost("{id:guid}/lida")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _notifications.MarkReadAsync(User.GetCompanyId(), id, cancellationToken);
        return NoContent();
    }

    /// <summary>POST /api/notificacoes/lidas - marks every notification of the company as read.</summary>
    [HttpPost("lidas")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _notifications.MarkAllReadAsync(User.GetCompanyId(), cancellationToken);
        return NoContent();
    }
}
