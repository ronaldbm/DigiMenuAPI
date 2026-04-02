using DigiMenuAPI.Application.DTOs.Update;

public record SettingUpdateDto(
        int Id,
        int BranchId,
        IFormFile Logo,
        // Identidad
        string BusinessName,
        string? Tagline,
        string? MetaTitle,
        string? MetaDescription,
        string? GoogleAnalyticsId,
        string? FacebookPixelId,
        // Tema visual — paletas de color
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
        bool ShowContactButton,
        bool ShowModalProductInfo,
        // Categorías
        byte CategoryHeaderStyle,
        bool ShowCategoryImages,
        // Localización
        string CountryCode,
        string PhoneCode,
        string Currency,
        string CurrencyLocale,
        string Language,
        string TimeZone,
        byte Decimals,
        // Formulario reservas
        bool FormShowName,
        bool FormRequireName,
        bool FormShowPhone,
        bool FormRequirePhone,
        bool FormShowTable,
        bool FormRequireTable,
        bool FormShowPersons,
        bool FormRequirePersons,
        bool FormShowAllergies,
        bool FormRequireAllergies,
        bool FormShowBirthday,
        bool FormRequireBirthday,
        bool FormShowComments,
        bool FormRequireComments
    );
