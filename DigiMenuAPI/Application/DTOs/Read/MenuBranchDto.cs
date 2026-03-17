namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// DTO raíz del menú público de una Branch.
    /// Agrupa todo lo que el frontend necesita para renderizar el menú completo.
    ///
    /// WeeklySchedule:      7 días ordenados Lun-Dom. Null si no hay horario configurado.
    /// UpcomingSpecialDays: días especiales desde hoy hasta 30 días. Null si no hay ninguno.
    /// </summary>
    public record MenuBranchDto(
        // ── Identidad — BranchInfo ────────────────────────────────────
        string BusinessName,
        string? Tagline,
        string? LogoUrl,
        string? FaviconUrl,
        string? BackgroundImageUrl,
        // ── Tema visual — BranchTheme ─────────────────────────────────
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
        // ── Layout — BranchTheme ──────────────────────────────────────
        byte HeaderStyle,
        byte MenuLayout,
        byte ProductDisplay,
        bool ShowProductDetails,
        byte FilterMode,
        bool ShowContactButton,
        bool ShowModalProductInfo,
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

        /// <summary>
        /// Los 7 días del horario semanal ordenados Lun-Dom.
        /// Null si la Branch no tiene horario configurado aún.
        /// </summary>
        List<BranchScheduleReadDto>? WeeklySchedule,

        /// <summary>
        /// Días especiales desde hoy hasta 30 días adelante.
        /// Null si no hay días especiales próximos.
        /// </summary>
        List<BranchSpecialDayReadDto>? UpcomingSpecialDays
    );
}