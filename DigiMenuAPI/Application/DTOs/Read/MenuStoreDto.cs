using DigiMenuAPI.Application.DTOs.Read;

public record MenuStoreDto(
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
       string Currency,
       string CurrencyLocale,
       string Language,
       byte Decimals,

       // SEO
       string? MetaTitle,
       string? MetaDescription,

       // Analytics
       string? GoogleAnalyticsId,
       string? FacebookPixelId,

       // Contenido
       List<CategoryReadDto> Categories,
       List<FooterLinkReadDto> FooterLinks,

       // Módulos activos (para que el frontend sepa qué mostrar)
       List<string> ActiveModules
   )
{
    public MenuStoreDto() : this(
        string.Empty, null, null, null, null,
        false, "#FFFFFF", "#FFFFFF", "#000000", "#000000", "#FFFFFF",
        "#E63946", "#FFFFFF", "#457B9D", "#000000", "#1D3557", "#FFFFFF",
        1, 1, 1, true, true, true,
        "CO", "USD", "en-US", "ES", 2,
        null, null, null, null,
        new List<CategoryReadDto>(), new List<FooterLinkReadDto>(),
        new List<string>()
    )
    { }
}