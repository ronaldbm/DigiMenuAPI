namespace DigiMenuAPI.Application.DTOs.Read;

public record AccountSplitItemReadDto(
    int     Id,
    int     AccountSplitId,
    int     AccountItemId,
    string  ProductName,
    decimal UnitPrice,
    decimal Quantity,
    decimal LineTotal
);
