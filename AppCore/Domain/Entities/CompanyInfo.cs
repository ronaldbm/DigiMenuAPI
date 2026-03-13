using System.ComponentModel.DataAnnotations;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Identidad visual del negocio para una Company.
    /// Contiene el nombre comercial, slogan e imágenes de marca.
    ///
    /// Separada de CompanyTheme para permitir actualizar identidad
    /// sin tocar colores ni layout, y viceversa.
    ///
    /// Relación 1:1 con Company.
    /// </summary>
    public class CompanyInfo : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // ── Identidad del negocio ─────────────────────────────────────
        [Required, MaxLength(100)]
        public string BusinessName { get; set; } = null!;

        [MaxLength(200)]
        public string? Tagline { get; set; }

        /// <summary>URL del logo principal. Gestionado por FileStorageService.</summary>
        public string? LogoUrl { get; set; }

        /// <summary>URL del favicon. Gestionado por FileStorageService.</summary>
        public string? FaviconUrl { get; set; }

        /// <summary>URL de imagen de fondo del menú. Gestionado por FileStorageService.</summary>
        public string? BackgroundImageUrl { get; set; }
    }
}
