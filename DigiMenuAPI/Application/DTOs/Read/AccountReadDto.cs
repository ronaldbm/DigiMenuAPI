using DigiMenuAPI.Application.Common.Enums;

namespace DigiMenuAPI.Application.DTOs.Read;

public record AccountReadDto(
    int      Id,
    int      BranchId,
    string   ClientIdentifier,
    AccountStatus Status,
    string   StatusLabel,
    string?  Notes,
    int?     TabAuthorizedByUserId,
    int?     CustomerId,
    string?  CustomerName,
    DateTime CreatedAt,
    decimal  TotalAmount,
    int      ItemCount
);
