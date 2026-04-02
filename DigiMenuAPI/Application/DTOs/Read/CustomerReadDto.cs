namespace DigiMenuAPI.Application.DTOs.Read
{
    public record CustomerReadDto(
        int      Id,
        int      CompanyId,
        string   Name,
        string?  Phone,
        string?  Email,
        string?  Notes,
        decimal  CreditLimit,
        decimal  CurrentBalance,
        int      MaxOpenTabs,
        decimal  MaxTabAmount,
        bool     IsActive,
        int      OpenAccountsCount,
        int      TabAccountsCount,
        DateTime CreatedAt
    );
}
