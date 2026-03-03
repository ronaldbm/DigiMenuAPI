using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Catálogo global de módulos disponibles en la plataforma.
    /// Solo el SuperAdmin puede agregar/editar módulos del catálogo.
    /// </summary>
    public class PlatformModule
    {
        public int Id { get; set; }

        /// <summary>
        /// Código único del módulo. Usado en código para verificar acceso.
        /// Ejemplos: "RESERVATIONS", "TABLE_MANAGEMENT", "ANALYTICS", "ONLINE_ORDERS"
        /// </summary>
        [Required, MaxLength(50)]
        public string Code { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(300)]
        public string? Description { get; set; }

        /// <summary>Si el módulo está disponible para asignar (soft disable global).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Orden visual en el panel de administración.</summary>
        public int DisplayOrder { get; set; }

        // Navegación
        public ICollection<CompanyModule> CompanyModules { get; set; } = new List<CompanyModule>();
    }
}


// DigiMenuAPI/Infrastructure/Entities/CompanyModule.cs
namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Módulos activados para una empresa específica.
    /// El SuperAdmin gestiona estas activaciones manualmente.
    /// </summary>
    public class CompanyModule
    {
        public int Id { get; set; }

        // ── CLAVES FORÁNEAS ──────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        public int PlatformModuleId { get; set; }
        public PlatformModule PlatformModule { get; set; } = null!;

        // ── ESTADO ──────────────────────────────────────────────────
        public bool IsActive { get; set; } = true;

        public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>SuperAdmin que activó el módulo.</summary>
        public int ActivatedByUserId { get; set; }

        /// <summary>Fecha de expiración. Null = sin vencimiento.</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>Notas internas del SuperAdmin (motivo, plan, etc.).</summary>
        public string? Notes { get; set; }
    }
}


// DigiMenuAPI/Application/Common/ModuleCodes.cs
namespace DigiMenuAPI.Application.Common
{
    /// <summary>
    /// Constantes de códigos de módulos. Usar siempre estas constantes
    /// en el código en lugar de strings literales.
    /// </summary>
    public static class ModuleCodes
    {
        public const string Reservations     = "RESERVATIONS";
        public const string TableManagement  = "TABLE_MANAGEMENT";
        public const string Analytics        = "ANALYTICS";
        public const string OnlineOrders     = "ONLINE_ORDERS";
    }
}
