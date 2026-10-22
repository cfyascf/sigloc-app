namespace Sigloc.Application.DTOs;

/// <summary>Transport-agnostic upload so the application layer never depends on ASP.NET's IFormFile.</summary>
public sealed record UploadedFileDto(Stream Content, string? FileName, long Length);
