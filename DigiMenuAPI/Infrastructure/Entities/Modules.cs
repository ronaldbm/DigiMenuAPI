using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Módulo funcional disponible en la plataforma (catálogo global).
    /// Ejemplos: RESERVATIONS, TABLE_MANAGEMENT, ANALYTICS, ONLINE_ORDERS
    /// Los módulos los gestiona el SuperAdmin y los activa por empresa.
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
    /// Módulo activado para una empresa específica (tabla de relación).
    /// Una empresa no puede tener el mismo módulo dos veces (índice único CompanyId + PlatformModuleId).
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

        /// <summary>Id del usuario (SuperAdmin) que activó el módulo.</summary>
        public int ActivatedByUserId { get; set; }

        public DateTime? ExpiresAt { get; set; }
        public string? Notes { get; set; }
    }
}