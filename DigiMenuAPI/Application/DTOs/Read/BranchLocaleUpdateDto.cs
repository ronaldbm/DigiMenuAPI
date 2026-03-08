namespace DigiMenuAPI.Application.DTOs.Update
{
    public record BranchLocaleUpdateDto(
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