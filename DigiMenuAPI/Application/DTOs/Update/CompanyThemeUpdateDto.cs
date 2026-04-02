namespace DigiMenuAPI.Application.DTOs.Update
{
    public record ColorPaletteUpdateDto(
        string HeaderBackgroundColor,
        string HeaderTextColor,
        string PageBackgroundColor,
        string TextColor,
        string TitlesColor,
        string CardBackgroundColor,
        string CardBorderColor,
        string TabBackgroundColor,
        string TabTextColor,
        string PrimaryColor,
        string PrimaryTextColor,
        string SecondaryColor,
        string FooterBackgroundColor,
        string BrowserThemeColor
    );

    public record BackgroundSettingsUpdateDto(
        byte Opacity,
        byte Position,
        byte Size,
        bool Repeat
    );

    public record FrameSettingsUpdateDto(
        byte FrameId,
        string? CustomFrameUrl
    );

    public record CompanyThemeUpdateDto(
        // Paletas de color
        ColorPaletteUpdateDto ColorPalette,
        ColorPaletteUpdateDto? DarkModePalette,
        // Fondo y marco
        BackgroundSettingsUpdateDto BackgroundSettings,
        FrameSettingsUpdateDto FrameSettings,
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
