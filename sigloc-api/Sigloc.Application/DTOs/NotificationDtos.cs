using System.Text.Json.Serialization;

namespace Sigloc.Application.DTOs;

public record NotificationDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("tipo")] string Type,
    [property: JsonPropertyName("titulo")] string Title,
    [property: JsonPropertyName("mensagem")] string Message,
    [property: JsonPropertyName("link")] string? Link,
    [property: JsonPropertyName("lida")] bool Read,
    [property: JsonPropertyName("criadaEm")] DateTimeOffset CreatedAt);

public record NotificationListDto(
    [property: JsonPropertyName("itens")] IReadOnlyList<NotificationDto> Items,
    [property: JsonPropertyName("naoLidas")] int Unread);

public record UnreadNotificationsDto([property: JsonPropertyName("naoLidas")] int Unread);
