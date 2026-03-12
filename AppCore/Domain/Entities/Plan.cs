using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Plan de suscripción de la plataforma.
    /// El SuperAdmin gestiona los planes y asigna uno a cada Company al darla de alta.
    /// Los límites se aplican en los servicios correspondientes al crear Branches y Users.
    ///
    /// Convención de valores especiales:
    ///   -1 en MaxBranches o MaxUsers = ilimitado (plan Enterprise).
    /// </summary>
    public class Plan
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Código único del plan. Ejemplo: "BASIC", "PRO", "BUSINESS", "ENTERPRISE".
        /// Se usa en validaciones y lógica de negocio para evitar depender del Id.
        /// </summary>
        [Required, MaxLength(50)]
        public string Code { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(300)]
        public string? Description { get; set; }

        // ── Precios ───────────────────────────────────────────────────
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyPrice { get; set; }

        /// <summary>
        /// Precio por mes cuando se paga de forma anual (normalmente con descuento).
        /// Null = no ofrece facturación anual.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AnnualPrice { get; set; }

        // ── Límites del plan ──────────────────────────────────────────
        /// <summary>Máximo de sucursales. -1 = ilimitado.</summary>
        public int MaxBranches { get; set; }

        /// <summary>
        /// Pool total de usuarios de la empresa (CompanyAdmin + BranchAdmins + Staff).
        /// -1 = ilimitado.
        /// </summary>
        public int MaxUsers { get; set; }

        // ── Visibilidad ───────────────────────────────────────────────
        /// <summary>Si el plan aparece en la página de precios pública.</summary>
        public bool IsPublic { get; set; } = true;

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }

        // ── Navegación ───────────────────────────────────────────────
        public ICollection<Company> Companies { get; set; } = new List<Company>();
    }
}
