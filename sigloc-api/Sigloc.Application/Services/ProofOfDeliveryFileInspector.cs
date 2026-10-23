using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;

namespace Sigloc.Application.Services;

public sealed record InspectedFile(byte[] Content, string ContentType, string Extension, string FileName);

/// <summary>
/// Validates a proof-of-delivery upload by its content signature. The client-declared name and content
/// type are never trusted: only JPEG, PNG and PDF signatures are accepted, up to <see cref="MaxBytes"/>.
/// </summary>
public static class ProofOfDeliveryFileInspector
{
    public const long MaxBytes = 10 * 1024 * 1024;
    private const string Field = "comprovante";

    private static readonly (byte[] Signature, string ContentType, string Extension)[] Known =
    [
        ("%PDF-"u8.ToArray(), "application/pdf", ".pdf"),
        ([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "image/png", ".png"),
        ([0xFF, 0xD8, 0xFF], "image/jpeg", ".jpg")
    ];

    public static async Task<InspectedFile> InspectAsync(UploadedFileDto file, CancellationToken ct = default)
    {
        if (file.Length > MaxBytes) Reject("O comprovante deve ter no máximo 10 MB.");
        using var buffer = new MemoryStream();
        // Read at most one byte past the limit so a lying Content-Length cannot bypass it.
        var chunk = new byte[81920];
        int read;
        while ((read = await file.Content.ReadAsync(chunk, ct)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > MaxBytes) Reject("O comprovante deve ter no máximo 10 MB.");
        }
        if (buffer.Length == 0) Reject("O comprovante está vazio.");

        var content = buffer.ToArray();
        var match = Known.FirstOrDefault(k => content.AsSpan().StartsWith(k.Signature));
        if (match.ContentType == null) Reject("Envie o comprovante em JPG, PNG ou PDF.");
        return new InspectedFile(content, match.ContentType!, match.Extension!, SafeName(file.FileName, match.Extension!));
    }

    private static string SafeName(string? name, string extension)
    {
        var baseName = Path.GetFileNameWithoutExtension(Path.GetFileName(name ?? string.Empty).Replace('\\', '/').Split('/')[^1]);
        var cleaned = new string(baseName.Where(c => !char.IsControl(c) && !Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim();
        if (cleaned.Length == 0) cleaned = "comprovante";
        return (cleaned.Length > 200 ? cleaned[..200] : cleaned) + extension;
    }

    private static void Reject(string reason) =>
        throw new ValidationException([new ValidationError(Field, reason)], "Comprovante de entrega inválido.");
}
