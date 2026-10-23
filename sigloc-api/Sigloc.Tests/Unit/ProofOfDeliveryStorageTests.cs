using Microsoft.Extensions.Options;
using Sigloc.Application.DTOs;
using Sigloc.Application.Exceptions;
using Sigloc.Application.Services;
using Sigloc.Infrastructure.Storage;

namespace Sigloc.Tests.Unit;

public class ProofOfDeliveryFileInspectorTests
{
    private static readonly byte[] Pdf = [.. "%PDF-1.7\n"u8.ToArray(), 1, 2, 3];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0];

    public static TheoryData<byte[], string, string> Accepted => new()
    {
        { Pdf, "application/pdf", ".pdf" },
        { Png, "image/png", ".png" },
        { Jpeg, "image/jpeg", ".jpg" }
    };

    [Theory]
    [MemberData(nameof(Accepted))]
    public async Task Detects_type_from_signature_and_ignores_client_name(byte[] content, string type, string extension)
    {
        var result = await ProofOfDeliveryFileInspector.InspectAsync(Upload(content, "canhoto.exe"));

        result.ContentType.Should().Be(type);
        result.Extension.Should().Be(extension);
        result.FileName.Should().Be("canhoto" + extension);
        result.Content.Should().Equal(content);
    }

    [Fact]
    public async Task Rejects_executable_renamed_as_pdf()
    {
        var act = () => ProofOfDeliveryFileInspector.InspectAsync(Upload([0x4D, 0x5A, 0x90, 0x00], "canhoto.pdf"));

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle(e => e.Field == "comprovante");
    }

    [Fact]
    public async Task Rejects_empty_file() =>
        await ((Func<Task>)(() => ProofOfDeliveryFileInspector.InspectAsync(Upload([], "a.pdf")))).Should().ThrowAsync<ValidationException>();

    [Fact]
    public async Task Rejects_content_over_limit_even_when_declared_length_is_small()
    {
        var content = new byte[ProofOfDeliveryFileInspector.MaxBytes + 1];
        Pdf.CopyTo(content, 0);
        var upload = new UploadedFileDto(new MemoryStream(content), "a.pdf", 10);

        await ((Func<Task>)(() => ProofOfDeliveryFileInspector.InspectAsync(upload))).Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("../../etc/passwd.pdf", "passwd.pdf")]
    [InlineData("C:\\temp\\nota fiscal.pdf", "nota fiscal.pdf")]
    [InlineData("   ", "comprovante.pdf")]
    [InlineData(null, "comprovante.pdf")]
    public async Task Display_name_is_sanitized(string? name, string expected) =>
        (await ProofOfDeliveryFileInspector.InspectAsync(Upload(Pdf, name))).FileName.Should().Be(expected);

    private static UploadedFileDto Upload(byte[] content, string? name) => new(new MemoryStream(content), name, content.Length);
}

public sealed class LocalDiskFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sigloc-storage-" + Guid.NewGuid().ToString("N"));
    private LocalDiskFileStorage Storage => new(Options.Create(new FileStorageSettings { RootPath = _root }));

    [Fact]
    public async Task Saves_reads_and_deletes_under_generated_key()
    {
        var key = await Storage.SaveAsync("trips/abc", new MemoryStream([1, 2, 3]), ".pdf");

        key.Should().StartWith("trips/abc/").And.EndWith(".pdf");
        await using (var stream = await Storage.OpenReadAsync(key))
        {
            using var copy = new MemoryStream();
            await stream!.CopyToAsync(copy);
            copy.ToArray().Should().Equal(1, 2, 3);
        }
        Directory.GetFiles(_root, "*.tmp", SearchOption.AllDirectories).Should().BeEmpty();

        await Storage.DeleteAsync(key);
        (await Storage.OpenReadAsync(key)).Should().BeNull();
    }

    [Theory]
    [InlineData("../outside.pdf")]
    [InlineData("trips/../../outside.pdf")]
    public async Task Keys_resolving_outside_root_are_rejected(string key) =>
        await ((Func<Task>)(() => Storage.OpenReadAsync(key))).Should().ThrowAsync<ArgumentException>();

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
