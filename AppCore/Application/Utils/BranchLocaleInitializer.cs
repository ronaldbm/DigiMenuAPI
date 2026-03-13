using AppCore.Domain.Entities;

namespace AppCore.Application.Utils
{
    /// <summary>
    /// Crea una BranchLocale con valores por defecto derivados del país.
    /// Centraliza la lógica para evitar duplicación entre AuthService y BranchService.
    /// </summary>
    public static class BranchLocaleInitializer
    {
        public static BranchLocale Create(int branchId, string? countryCode)
        {
            return new BranchLocale
            {
                BranchId = branchId,
                CountryCode = countryCode?.ToUpper() ?? "CR",
                PhoneCode = LocaleHelper.ResolvePhoneCode(countryCode),
                Currency = LocaleHelper.ResolveCurrency(countryCode),
                CurrencyLocale = LocaleHelper.ResolveCurrencyLocale(countryCode),
                Language = "es",
                TimeZone = LocaleHelper.ResolveTimeZone(countryCode),
                Decimals = 2
            };
        }
    }
}
