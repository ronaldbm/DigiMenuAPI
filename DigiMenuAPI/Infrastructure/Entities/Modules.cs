using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Módulo funcional disponible en la plataforma (catálogo global).
    /// Ejemplos: RESERVATIONS, TABLE_MANAGEMENT, ANALYTICS, ONLINE_ORDERS.
    /// Los módulos los gestiona el SuperAdmin y se activan por Company desde un panel para SuperAdmin.
    /// Una vez activos para la Company, aplican a todas sus Branches.
    /// </summary>
    public class PlatformModule
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Código único del módulo. Ejemplo: "RESERVATIONS"</summary>
        [Required, MaxLength(50)]
        public string Code { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(300)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Módulo activado para una empresa específica.
    /// Una empresa no puede tener el mismo módulo dos veces (índice único CompanyId + PlatformModuleId).
    /// La activación aplica a toda la Company y por ende a todas sus Branches.
    /// </summary>
    public class CompanyModule
    {
        [Key]
        public int Id { get; set; }

        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        public int PlatformModuleId { get; set; }
        public PlatformModule PlatformModule { get; set; } = null!;

        public bool IsActive { get; set; } = true;
        public DateTime ActivatedAt { get; set; }

        /// <summary>Id del AppUser (SuperAdmin) que activó el módulo.</summary>
        public int ActivatedByUserId { get; set; }

        /// <summary>Fecha de vencimiento del módulo. Null = sin vencimiento.</summary>
        public DateTime? ExpiresAt { get; set; }

        public string? Notes { get; set; }
    }
}
