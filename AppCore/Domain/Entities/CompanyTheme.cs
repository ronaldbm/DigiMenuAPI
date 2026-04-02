using System.ComponentModel.DataAnnotations;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Configuración visual y de layout del menú público de una Company.
    ///
    /// Colores almacenados como JSON (ColorPalette, DarkModePalette) para
    /// evitar migraciones al agregar nuevos colores.
    /// DarkModePalette = null → generar automáticamente desde ColorPalette.
    ///
    /// Relación 1:1 con Company.
    /// </summary>
    public class CompanyTheme : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // ── Paleta de colores (JSON) ───────────────────────────────────
        /// <summary>Paleta de colores para modo claro.</summary>
        public ColorPaletteData ColorPalette { get; set; } = new();

        /// <summary>
        /// Overrides de colores para modo oscuro.
        /// Null = auto-generar paleta oscura desde ColorPalette.
        /// Puede tener solo los campos que el usuario quiso personalizar.
        /// </summary>
        public ColorPaletteData? DarkModePalette { get; set; }

        // ── Configuración de fondo (JSON) ─────────────────────────────
        /// <summary>Controles de la imagen de fondo. Solo aplica si BackgroundImageUrl != null.</summary>
        public BackgroundSettingsData BackgroundSettings { get; set; } = new();

        // ── Marco decorativo (JSON) ────────────────────────────────────
        /// <summary>Configuración del marco decorativo. FrameId=0 = sin marco.</summary>
        public FrameSettingsData FrameSettings { get; set; } = new();

        // ── Modo oscuro ───────────────────────────────────────────────
        public bool IsDarkMode { get; set; }

        /// <summary>
        /// Si true, la paleta oscura se auto-genera desde ColorPalette preservando identidad de marca.
        /// Si false, se usa DarkModePalette directamente (o los colores claros como fallback).
        /// </summary>
        public bool DarkModeAutoGenerate { get; set; } = true;

        // ── Layout ────────────────────────────────────────────────────
        /// <summary>Estilo del header. 1: Logo+Nombre izq, 2: Centrado, 3: Solo nombre, 4: Solo logo.</summary>
        public byte HeaderStyle { get; set; } = 1;

        /// <summary>Distribución del menú. 1: Por categorías, 2: Lista plana.</summary>
        public byte MenuLayout { get; set; } = 1;

        /// <summary>Visualización de productos. 1: Grid, 2: List, 3: Compact.</summary>
        public byte ProductDisplay { get; set; } = 1;

        // ── Encabezados de categoría ──────────────────────────────────
        /// <summary>Estilo global de encabezado. 1: AccentBar, 2: Underline, 3: Filled, 4: Minimal.</summary>
        public byte CategoryHeaderStyle { get; set; } = 1;

        /// <summary>Si false, no se entregan HeaderImageUrl de categorías al menú público.</summary>
        public bool ShowCategoryImages { get; set; } = true;

        // ── Comportamiento ────────────────────────────────────────────
        public bool ShowProductDetails { get; set; } = true;

        /// <summary>Modo de filtro del menú. 0: Sin filtro, 1: Por etiquetas, 2: Por categorías.</summary>
        public byte FilterMode { get; set; } = 0;
        public bool ShowContactButton { get; set; }
        public bool ShowModalProductInfo { get; set; }
        public bool ShowMapInMenu { get; set; } = true;
    }
}
