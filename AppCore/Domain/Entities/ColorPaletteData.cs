namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Paleta de colores del menú público, almacenada como JSON en CompanyTheme.
    /// Soporta colores en formato HEX (#RRGGBB o #RRGGBBAA).
    /// Al agregar nuevos colores: añadir propiedad con default seguro (no requiere migración).
    /// </summary>
    public class ColorPaletteData
    {
        // ── Cabecera ──────────────────────────────────────────────────
        public string HeaderBackgroundColor { get; set; } = "#FFFFFF";
        public string HeaderTextColor { get; set; } = "#000000";

        // ── Cuerpo ────────────────────────────────────────────────────
        public string PageBackgroundColor { get; set; } = "#FFFFFF";
        public string TextColor { get; set; } = "#1D3557";
        public string TitlesColor { get; set; } = "#000000";

        // ── Tarjetas de producto ──────────────────────────────────────
        public string CardBackgroundColor { get; set; } = "#FFFFFF";
        public string CardBorderColor { get; set; } = "#0F0F0F0F";

        // ── Filtros / Tabs ────────────────────────────────────────────
        public string TabBackgroundColor { get; set; } = "#000000";
        public string TabTextColor { get; set; } = "#FFFFFF";

        // ── Acentos de marca ──────────────────────────────────────────
        public string PrimaryColor { get; set; } = "#E63946";
        public string PrimaryTextColor { get; set; } = "#FFFFFF";
        public string SecondaryColor { get; set; } = "#457B9D";

        // ── Pie de página ─────────────────────────────────────────────
        public string FooterBackgroundColor { get; set; } = "#FFFFFF";

        // ── Navegador (mobile address bar) ────────────────────────────
        public string BrowserThemeColor { get; set; } = "#FFFFFF";
    }
}
