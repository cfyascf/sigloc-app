using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigloc.Api.Extensions;
using Sigloc.Application.Contracts;
using Sigloc.Application.DTOs;
using Sigloc.Domain.Constants;

namespace Sigloc.Api.Controllers;

/// <summary>Active trips of the authenticated carrier: tracking, milestones, occurrences and POD.</summary>
[ApiController]
[Route("api/transportador/viagens")]
[Authorize(Policy = Policies.RequireCarrierAccess)]
public sealed class CarrierTripsController(ICarrierTripService service) : ControllerBase
{
    // Slightly above the 10 MB file limit so oversized files get a clear validation error, not a reset.
    private const long MaxUploadBytes = 11 * 1024 * 1024;

    [HttpGet("ativas")]
    public async Task<IActionResult> Search([FromQuery] TripQueryDto query, CancellationToken ct) =>
        Ok(await service.SearchAsync(User.GetCompanyId(), query, ct));

    /// <summary>Trip detail; <c>refresh=true</c> is the driver's "Transmitir Posição GPS" ping.</summary>
    [HttpGet("{id:guid}/detalhes")]
    public async Task<IActionResult> Detail(Guid id, [FromQuery] bool refresh, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await service.GetDetailAsync(User.GetCompanyId(), id, refresh, ct));
    }

    [HttpPost("{id:guid}/ocorrencias")]
    public async Task<IActionResult> ReportOccurrence(Guid id, [FromBody] ReportTripOccurrenceRequestDto request, CancellationToken ct)
    {
        var result = await service.ReportOccurrenceAsync(User.GetCompanyId(), User.GetUserId(), id, request, ct);
        return CreatedAtAction(nameof(Detail), new { id }, result);
    }

    /// <summary>Confirms the next pending stop; deliveries require the POD file in <c>comprovante</c>.</summary>
    [HttpPost("{id:guid}/paradas/{stopId:guid}/conclusao")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<IActionResult> CompleteStop(Guid id, Guid stopId,
        [FromForm(Name = "comprovante")] IFormFile? proof, [FromForm(Name = "observacao")] string? note, CancellationToken ct)
    {
        await using var content = proof?.OpenReadStream();
        var upload = proof == null ? null : new UploadedFileDto(content!, proof.FileName, proof.Length);
        return Ok(await service.CompleteStopAsync(User.GetCompanyId(), User.GetUserId(), id, stopId, upload, note, ct));
    }

    [HttpGet("{id:guid}/comprovantes/{proofId:guid}")]
    public async Task<IActionResult> DownloadProof(Guid id, Guid proofId, CancellationToken ct)
    {
        var file = await service.OpenProofAsync(User.GetCompanyId(), id, proofId, ct);
        Response.Headers.CacheControl = "no-store";
        return File(file.Content, file.ContentType, file.FileName);
    }
}
