using System.ComponentModel.DataAnnotations;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Identidad visual del negocio para una Branch.
    /// Contiene el nombre comercial, slogan e imágenes de marca.
    ///
    /// Separada de BranchTheme para permitir actualizar identidad
    /// sin tocar colores ni layout, y viceversa.
    ///
    /// Relación 1:1 con Branch.
    /// </summary>
    public class BranchInfo : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

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
