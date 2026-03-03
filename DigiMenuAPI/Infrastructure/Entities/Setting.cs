using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Configuración completa del tenant: branding, tema, localización,
    /// comportamiento del menú y formulario de reservas.
    /// Relación 1:1 con Company.
    /// </summary>
    public class Setting : BaseEntity
    {
        // ════════════════════════════════════════════════════════════
        // IDENTIDAD / BRANDING
        // ════════════════════════════════════════════════════════════

        [Required, MaxLength(100)]
        public string BusinessName { get; set; } = null!;

        /// <summary>Tagline o slogan visible bajo el nombre del negocio.</summary>
        [MaxLength(200)]
        public string? Tagline { get; set; }

        public string? LogoUrl { get; set; }
        public string? FaviconUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }

        // ════════════════════════════════════════════════════════════
        // TEMA / COLORES
        // ════════════════════════════════════════════════════════════

        /// <summary>Modo oscuro activado.</summary>
        public bool IsDarkMode { get; set; } = false;

        /// <summary>Color de fondo general de la página (#hex).</summary>
        [MaxLength(7)]
        public string PageBackgroundColor { get; set; } = "#FFFFFF";

        /// <summary>Color de fondo del header.</summary>
        [MaxLength(7)]
        public string HeaderBackgroundColor { get; set; } = "#FFFFFF";

        /// <summary>Color del texto del header.</summary>
        [MaxLength(7)]
        public string HeaderTextColor { get; set; } = "#000000";

        /// <summary>Color de fondo de la barra de categorías/tabs.</summary>
        [MaxLength(7)]
        public string TabBackgroundColor { get; set; } = "#000000";

        /// <summary>Color del texto de la barra de categorías/tabs.</summary>
        [MaxLength(7)]
        public string TabTextColor { get; set; } = "#FFFFFF";

        /// <summary>Color principal (CTA, botones primarios).</summary>
        [MaxLength(7)]
        public string PrimaryColor { get; set; } = "#E63946";

        /// <summary>Color del texto sobre el botón primario.</summary>
        [MaxLength(7)]
        public string PrimaryTextColor { get; set; } = "#FFFFFF";

        /// <summary>Color secundario (acentos, highlights).</summary>
        [MaxLength(7)]
        public string SecondaryColor { get; set; } = "#457B9D";

        /// <summary>Color de los títulos de sección.</summary>
        [MaxLength(7)]
        public string TitlesColor { get; set; } = "#000000";

        /// <summary>Color general del texto de la página.</summary>
        [MaxLength(7)]
        public string TextColor { get; set; } = "#1D3557";

        /// <summary>Color del meta tag theme-color del navegador.</summary>
        [MaxLength(7)]
        public string BrowserThemeColor { get; set; } = "#FFFFFF";

        // ════════════════════════════════════════════════════════════
        // HEADER / LAYOUT
        // ════════════════════════════════════════════════════════════

        /// <summary>Estilo del header: 1=Slim, 2=Full, 3=Banner</summary>
        public byte HeaderStyle { get; set; } = 1;

        /// <summary>Layout de navegación del menú: 1=Tabs, 2=Sidebar, 3=Scroll</summary>
        public byte MenuLayout { get; set; } = 1;

        /// <summary>Layout de las tarjetas de producto: 1=Grid, 2=List, 3=Compact</summary>
        public byte ProductDisplay { get; set; } = 1;

        /// <summary>Mostrar el detalle completo del producto al hacer click.</summary>
        public bool ShowProductDetails { get; set; } = true;

        /// <summary>Mostrar botón de búsqueda en el menú.</summary>
        public bool ShowSearchButton { get; set; } = true;

        /// <summary>Mostrar botón de contacto en el menú.</summary>
        public bool ShowContactButton { get; set; } = true;

        // ════════════════════════════════════════════════════════════
        // LOCALIZACIÓN
        // ════════════════════════════════════════════════════════════

        /// <summary>Código de país ISO 3166-1 alpha-2 (ej: "CO", "MX", "CR").</summary>
        [MaxLength(3)]
        public string CountryCode { get; set; } = "CO";

        /// <summary>Código telefónico con + (ej: "+57").</summary>
        [MaxLength(6)]
        public string PhoneCode { get; set; } = "+57";

        /// <summary>Código de moneda ISO 4217 (ej: "COP", "USD", "CRC").</summary>
        [MaxLength(5)]
        public string Currency { get; set; } = "USD";

        /// <summary>Locale para formatear moneda (ej: "es-CO", "en-US").</summary>
        [MaxLength(10)]
        public string CurrencyLocale { get; set; } = "en-US";

        /// <summary>Idioma principal del menú (ej: "ES", "EN").</summary>
        [MaxLength(5)]
        public string Language { get; set; } = "ES";

        /// <summary>Zona horaria IANA (ej: "America/Bogota").</summary>
        [MaxLength(50)]
        public string TimeZone { get; set; } = "America/Bogota";

        /// <summary>Decimales a mostrar en precios (0 o 2).</summary>
        public byte Decimals { get; set; } = 2;

        // ════════════════════════════════════════════════════════════
        // SEO / META
        // ════════════════════════════════════════════════════════════

        [MaxLength(100)]
        public string? MetaTitle { get; set; }

        [MaxLength(300)]
        public string? MetaDescription { get; set; }

        // ════════════════════════════════════════════════════════════
        // ANALYTICS
        // ════════════════════════════════════════════════════════════

        [MaxLength(50)]
        public string? GoogleAnalyticsId { get; set; }

        [MaxLength(50)]
        public string? FacebookPixelId { get; set; }

        // ════════════════════════════════════════════════════════════
        // FORMULARIO DE RESERVAS (campo por campo)
        // ════════════════════════════════════════════════════════════

        public bool FormShowName      { get; set; } = true;
        public bool FormRequireName   { get; set; } = true;

        public bool FormShowPhone     { get; set; } = true;
        public bool FormRequirePhone  { get; set; } = false;

        public bool FormShowTable     { get; set; } = false;
        public bool FormRequireTable  { get; set; } = false;

        public bool FormShowPersons   { get; set; } = true;
        public bool FormRequirePersons { get; set; } = true;

        public bool FormShowAllergies  { get; set; } = false;
        public bool FormRequireAllergies { get; set; } = false;

        public bool FormShowBirthday   { get; set; } = false;
        public bool FormRequireBirthday { get; set; } = false;

        public bool FormShowComments   { get; set; } = true;
        public bool FormRequireComments { get; set; } = false;

        // ════════════════════════════════════════════════════════════
        // TENANT FK
        // ════════════════════════════════════════════════════════════

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}
