using DigiMenuAPI.Application.Common.Enums;

namespace DigiMenuAPI.Application.DTOs.Read;

public record AccountDetailReadDto(
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
    List<AccountItemReadDto>     Items,
    List<AccountDiscountReadDto> Discounts,
    List<AccountSplitReadDto>    Splits,
    decimal  Subtotal,
    decimal  TotalDiscounts,
    decimal  Total
);
