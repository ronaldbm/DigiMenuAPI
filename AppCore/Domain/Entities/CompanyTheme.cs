using System.ComponentModel.DataAnnotations;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Configuración visual y de layout del menú público de una Company.
    /// Incluye paleta de colores, modo oscuro y estructura de presentación.
    ///
    /// Se actualiza frecuentemente durante la personalización del menú.
    /// Separada de CompanyInfo para no mezclar identidad con apariencia.
    ///
    /// Relación 1:1 con Company.
    /// </summary>
    public class CompanyTheme : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // ── Modo ──────────────────────────────────────────────────────
        public bool IsDarkMode { get; set; }

        // ── Paleta de colores ─────────────────────────────────────────
        [Required, MaxLength(7)]
        public string PageBackgroundColor { get; set; } = "#FFFFFF";

        [Required, MaxLength(7)]
        public string HeaderBackgroundColor { get; set; } = "#FFFFFF";

        [Required, MaxLength(7)]
        public string HeaderTextColor { get; set; } = "#000000";

        [Required, MaxLength(7)]
        public string TabBackgroundColor { get; set; } = "#000000";

        [Required, MaxLength(7)]
        public string TabTextColor { get; set; } = "#FFFFFF";

        [Required, MaxLength(7)]
        public string PrimaryColor { get; set; } = "#E63946";

        [Required, MaxLength(7)]
        public string PrimaryTextColor { get; set; } = "#FFFFFF";

        [Required, MaxLength(7)]
        public string SecondaryColor { get; set; } = "#457B9D";

        [Required, MaxLength(7)]
        public string TitlesColor { get; set; } = "#000000";

        [Required, MaxLength(7)]
        public string TextColor { get; set; } = "#1D3557";

        [Required, MaxLength(7)]
        public string BrowserThemeColor { get; set; } = "#FFFFFF";

        // ── Layout ────────────────────────────────────────────────────
        /// <summary>Estilo del header. 1: Estilo A, 2: Estilo B, 3: Estilo C.</summary>
        public byte HeaderStyle { get; set; } = 1;

        /// <summary>Distribución del menú. 1: Por categorías, 2: Lista plana.</summary>
        public byte MenuLayout { get; set; } = 1;

        /// <summary>Visualización de productos. 1: Grid, 2: List, 3: Compact.</summary>
        public byte ProductDisplay { get; set; } = 1;

        // ── Comportamiento ────────────────────────────────────────────
        public bool ShowProductDetails { get; set; } = true;
        public bool ShowSearchButton { get; set; }
        public bool ShowContactButton { get; set; }
    }
}
