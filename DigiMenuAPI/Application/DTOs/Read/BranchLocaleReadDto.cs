namespace DigiMenuAPI.Application.DTOs.Read
{
    public record BranchLocaleReadDto(
        int Id,
        int BranchId,
        string CountryCode,
        string PhoneCode,
        string Currency,
        string CurrencyLocale,
        string Language,
        string TimeZone,
        byte Decimals
    );
}