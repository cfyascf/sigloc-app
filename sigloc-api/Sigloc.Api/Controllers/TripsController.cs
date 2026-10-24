using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Api.Extensions;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;

namespace Sigloc.Api.Controllers;

[ApiController]
[Route("api/viagens")]
[Authorize(Policy = Policies.RequireShipperAccess)]
public sealed class TripsController(ITripMonitoringService service, ITripProofService proofs) : ControllerBase
{
    [HttpGet("ativas")]
    public async Task<IActionResult> Search([FromQuery] TripQueryDto query, CancellationToken ct) =>
        Ok(await service.SearchAsync(User.GetCompanyId(), query, ct));

    [HttpGet("{id:guid}/detalhes")]
    public async Task<IActionResult> Detail(Guid id, [FromQuery] bool refresh, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await service.GetDetailAsync(User.GetCompanyId(), id, refresh, ct));
    }

    /// <summary>Proof of delivery (canhoto) uploaded by the carrier, for the contractor that owns the trip.</summary>
    [HttpGet("{id:guid}/comprovantes/{proofId:guid}")]
    public async Task<IActionResult> DownloadProof(Guid id, Guid proofId, CancellationToken ct)
    {
        var file = await proofs.OpenAsync(TripScope.ForShipper(User.GetCompanyId()), id, proofId, ct);
        Response.Headers.CacheControl = "no-store";
        return File(file.Content, file.ContentType, file.FileName);
    }
}
