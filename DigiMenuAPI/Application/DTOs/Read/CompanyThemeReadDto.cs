namespace DigiMenuAPI.Application.DTOs.Read
{
    public record ColorPaletteDto(
        string HeaderBackgroundColor,
        string HeaderTextColor,
        string PageBackgroundColor,
        string TextColor,
        string CardBackgroundColor,
        string CardBorderColor,
        string TabBackgroundColor,
        string TabTextColor,
        string PrimaryColor,
        string PrimaryTextColor,
        string SecondaryColor,
        string FooterBackgroundColor,
        string FooterTextColor,
        string CategoryTitleColor,
        string CardTitleColor,
        string PriceColor,
        string PromotionColor,
        string BrowserThemeColor
    );

    public record BackgroundSettingsDto(
        byte Opacity,
        byte Position,
        byte Size,
        bool Repeat
    );

    public record FrameSettingsDto(
        byte FrameId,
        string? CustomFrameUrl
    );

    public record CompanyThemeReadDto(
        int Id,
        int CompanyId,
        // Paletas de color
        ColorPaletteDto ColorPalette,
        ColorPaletteDto? DarkModePalette,
        // Fondo y marco
        BackgroundSettingsDto BackgroundSettings,
        FrameSettingsDto FrameSettings,
        // Modo oscuro
        bool IsDarkMode,
        bool DarkModeAutoGenerate,
        // Layout
        byte HeaderStyle,
        byte MenuLayout,
        byte ProductDisplay,
        bool ShowProductDetails,
        byte FilterMode,
        // Categorías
        byte CategoryHeaderStyle,
        bool ShowCategoryImages,
        // Comportamiento
        bool ShowContactButton,
        bool ShowModalProductInfo,
        bool ShowMapInMenu
    );
}
