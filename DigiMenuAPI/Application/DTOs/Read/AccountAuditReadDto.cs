namespace DigiMenuAPI.Application.DTOs.Read;

public record AccountAuditReadDto(
    long     Id,
    int      AccountId,
    string   Action,
    int?     UserId,
    string?  UserName,
    string?  Details,
    string   HumanReadable,
    DateTime CreatedAt
);
