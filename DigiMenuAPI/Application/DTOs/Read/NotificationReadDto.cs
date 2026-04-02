namespace DigiMenuAPI.Application.DTOs.Read;

public record NotificationReadDto(
    int      Id,
    string   Type,
    string   Message,
    string?  RelatedEntity,
    int?     RelatedEntityId,
    bool     IsRead,
    DateTime CreatedAt
);
