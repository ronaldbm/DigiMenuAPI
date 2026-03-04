namespace DigiMenuAPI.Application.DTOs.Read
{ 
    /// <summary>
    /// DTO raíz del menú público de una Branch.
    /// Agrupa todo lo que el frontend necesita para renderizar el menú completo.
    /// </summary>
    public record MenuBranchDto(
        // Branding
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
        string Language,
        string Currency,
        string CurrencyLocale,
        byte Decimals,
        // SEO
        string? MetaTitle,
        string? MetaDescription,
        string? GoogleAnalyticsId,
        string? FacebookPixelId,
        // Contenido
        List<CategoryMenuDto> Categories,
        List<FooterLinkReadDto> FooterLinks
    );
}