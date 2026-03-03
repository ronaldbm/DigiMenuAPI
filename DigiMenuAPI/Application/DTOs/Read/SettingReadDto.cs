namespace DigiMenuAPI.Application.DTOs.Read
{
    public record SettingReadDto(
        // Identidad
        string BusinessName,
        string? Tagline,
        string? LogoUrl,
        string? FaviconUrl,
        string? BackgroundImageUrl,

        // Tema
        bool IsDarkMode,
        string PageBackgroundColor,
        string HeaderBackgroundColor,
        string HeaderTextColor,
        string TabBackgroundColor,
        string TabTextColor,
        string PrimaryColor,
        string PrimaryTextColor,
        string SecondaryColor,
        string TitlesColor,
        string TextColor,
        string BrowserThemeColor,

        // Layout
        byte HeaderStyle,
        byte MenuLayout,
        byte ProductDisplay,
        bool ShowProductDetails,
        bool ShowSearchButton,
        bool ShowContactButton,

        // Localización
        string CountryCode,
        string PhoneCode,
        string Currency,
        string CurrencyLocale,
        string Language,
        string TimeZone,
        byte Decimals,

        // SEO
        string? MetaTitle,
        string? MetaDescription,

        // Analytics
        string? GoogleAnalyticsId,
        string? FacebookPixelId,

        // Formulario reservas
        bool FormShowName, bool FormRequireName,
        bool FormShowPhone, bool FormRequirePhone,
        bool FormShowTable, bool FormRequireTable,
        bool FormShowPersons, bool FormRequirePersons,
        bool FormShowAllergies, bool FormRequireAllergies,
        bool FormShowBirthday, bool FormRequireBirthday,
        bool FormShowComments, bool FormRequireComments
    );

}
