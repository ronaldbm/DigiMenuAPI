namespace DigiMenuAPI.Application.DTOs.Read;

public record AccountSplitReadDto(
    int      Id,
    int      AccountId,
    string   SplitName,
    List<AccountSplitItemReadDto> Items,
    decimal  SplitTotal
);
