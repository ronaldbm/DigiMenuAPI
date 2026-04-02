namespace DigiMenuAPI.Application.DTOs.Read;

public record AccountItemReadDto(
    int      Id,
    int      AccountId,
    int      BranchProductId,
    string   ProductName,
    decimal  UnitPrice,
    int      Quantity,
    string?  Notes,
    decimal  LineTotal,
    List<AccountDiscountReadDto> AppliedDiscounts
);
