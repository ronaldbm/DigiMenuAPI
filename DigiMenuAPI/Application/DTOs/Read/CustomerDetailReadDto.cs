namespace DigiMenuAPI.Application.DTOs.Read
{
    public record CustomerDetailReadDto(
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
        decimal  TotalHistoricSpend,
        DateTime CreatedAt,
        DateTime? ModifiedAt
    );
}
