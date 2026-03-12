using AppCore.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Enlace del footer del menú público de una Branch.
    /// Cada sucursal gestiona sus propios enlaces (redes sociales, WhatsApp, web, etc).
    /// </summary>
    public class FooterLink : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Label { get; set; } = null!;

        [Required, MaxLength(500)]
        public string Url { get; set; } = null!;

        /// <summary>Icono del catálogo global de la plataforma. Null si usa CustomSvgContent.</summary>
        public int? StandardIconId { get; set; }
        public StandardIcon? StandardIcon { get; set; }

        /// <summary>SVG personalizado. Solo aplica si StandardIconId es null.</summary>
        public string? CustomSvgContent { get; set; }

        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; }
        public bool IsDeleted { get; set; }
    }
}
