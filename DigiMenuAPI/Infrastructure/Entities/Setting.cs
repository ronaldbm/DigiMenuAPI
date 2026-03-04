using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Configuración visual y de comportamiento de una Branch.
    /// Cada sucursal tiene su propio branding, colores, localización y comportamiento del menú.
    /// Relación 1:1 con Branch.
    /// </summary>
    public class Setting : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        /// <summary>Relación 1:1 con Branch.</summary>
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        // ── Identidad del negocio ─────────────────────────────────────
        [Required, MaxLength(100)]
        public string BusinessName { get; set; } = null!;

        [MaxLength(200)]
        public string? Tagline { get; set; }

        public string? LogoUrl { get; set; }
        public string? FaviconUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }

        // ── Tema visual ───────────────────────────────────────────────
        public bool IsDarkMode { get; set; }

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
        /// <summary>1: Estilo A, 2: Estilo B, 3: Estilo C</summary>
        public byte HeaderStyle { get; set; } = 1;

        /// <summary>1: Por categorías, 2: Lista plana</summary>
        public byte MenuLayout { get; set; } = 1;

        /// <summary>1: Grid, 2: List, 3: Compact</summary>
        public byte ProductDisplay { get; set; } = 1;

        public bool ShowProductDetails { get; set; }
        public bool ShowSearchButton { get; set; }
        public bool ShowContactButton { get; set; }

        // ── Localización ──────────────────────────────────────────────
        [Required, MaxLength(3)]
        public string CountryCode { get; set; } = "CR";

        [Required, MaxLength(6)]
        public string PhoneCode { get; set; } = "+506";

        [Required, MaxLength(5)]
        public string Currency { get; set; } = "CRC";

        [Required, MaxLength(10)]
        public string CurrencyLocale { get; set; } = "es-CR";

        [Required, MaxLength(5)]
        public string Language { get; set; } = "es";

        [Required, MaxLength(50)]
        public string TimeZone { get; set; } = "America/Costa_Rica";

        /// <summary>Cantidad de decimales para mostrar precios (0 = sin decimales).</summary>
        public byte Decimals { get; set; } = 2;

        // ── SEO / Analytics ───────────────────────────────────────────
        [MaxLength(100)]
        public string? MetaTitle { get; set; }

        [MaxLength(300)]
        public string? MetaDescription { get; set; }

        [MaxLength(50)]
        public string? GoogleAnalyticsId { get; set; }

        [MaxLength(50)]
        public string? FacebookPixelId { get; set; }

        // ── Formulario de Reservas ─────────────────────────────────────
        // Cada campo tiene una opción para mostrarlo y otra para requerirlo.
        // Si un campo no se muestra, tampoco puede requerirse.
        public bool FormShowName { get; set; } = true;
        public bool FormRequireName { get; set; } = true;

        public bool FormShowPhone { get; set; } = true;
        public bool FormRequirePhone { get; set; } = true;

        public bool FormShowTable { get; set; }
        public bool FormRequireTable { get; set; }

        public bool FormShowPersons { get; set; } = true;
        public bool FormRequirePersons { get; set; } = true;

        public bool FormShowAllergies { get; set; }
        public bool FormRequireAllergies { get; set; }

        public bool FormShowBirthday { get; set; }
        public bool FormRequireBirthday { get; set; }

        public bool FormShowComments { get; set; } = true;
        public bool FormRequireComments { get; set; }
    }
}
