namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// DTO raíz del menú público de una Branch.
    /// Agrupa todo lo que el frontend necesita para renderizar el menú completo.
    ///
    /// WeeklySchedule:      7 días ordenados Lun-Dom. Null si no hay horario configurado.
    /// UpcomingSpecialDays: días especiales desde hoy hasta 30 días. Null si no hay ninguno.
    /// BackgroundSettings:  null si no hay imagen de fondo configurada (optimización payload).
    /// FrameSettings:       null si FrameId=0 (sin marco).
    /// </summary>
    public record MenuBranchDto(
        // ── Identidad — BranchInfo ────────────────────────────────────
        string BusinessName,
        string? Tagline,
        string? LogoUrl,
        string? FaviconUrl,
        string? BackgroundImageUrl,
        // ── Tema visual — Colores ─────────────────────────────────────
        ColorPaletteDto ColorPalette,
        ColorPaletteDto? DarkModePalette,
        BackgroundSettingsDto? BackgroundSettings,
        FrameSettingsDto? FrameSettings,
        // ── Modo oscuro ───────────────────────────────────────────────
        bool IsDarkMode,
        bool DarkModeAutoGenerate,
        // ── Layout — BranchTheme ──────────────────────────────────────
        byte HeaderStyle,
        byte MenuLayout,
        byte ProductDisplay,
        bool ShowProductDetails,
        byte FilterMode,
        bool ShowContactButton,
        bool ShowModalProductInfo,
        // ── Categorías ────────────────────────────────────────────────
        byte CategoryHeaderStyle,
        bool ShowCategoryImages,
        // ── Localización — BranchLocale ───────────────────────────────
        string Language,
        string Currency,
        string CurrencyLocale,
        byte Decimals,
        // ── SEO — BranchSeo (opcional) ────────────────────────────────
        string? MetaTitle,
        string? MetaDescription,
        string? GoogleAnalyticsId,
        string? FacebookPixelId,
        // ── Contacto — Branch ─────────────────────────────────────────
        string? BranchPhone,
        string? BranchEmail,
        // ── Contenido dinámico ────────────────────────────────────────
        List<CategoryMenuDto> Categories,
        List<FooterLinkReadDto> FooterLinks,
        // ── Horario ───────────────────────────────────────────────────
        List<BranchScheduleReadDto>? WeeklySchedule,
        List<BranchSpecialDayReadDto>? UpcomingSpecialDays,
        List<CompanyLanguageReadDto> AvailableLanguages,
        // ── Geolocalización ───────────────────────────────────────────
        decimal? BranchLatitude,
        decimal? BranchLongitude,
        bool ShowMapInMenu
    );
}
