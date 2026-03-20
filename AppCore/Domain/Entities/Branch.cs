using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Sucursal / localización física de una Company.
    /// Cada Branch tiene su propio menú (BranchProducts), configuración visual,
    /// reservas, footer links y usuarios de staff.
    ///
    /// El Slug es único DENTRO de la Company (no globalmente).
    /// La URL pública se forma con ambos slugs:
    ///   {companySlug}.digimenu.cr/{branchSlug}
    ///
    /// Configuración separada en entidades 1:1:
    ///   BranchLocale   → Configuración regional
    ///   BranchReservationForm → Formulario de reservas (módulo RESERVATIONS)
    /// </summary>
    public class Branch : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // ── Datos de la sucursal ──────────────────────────────────────
        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Identificador único dentro de la Company para la URL pública.
        /// Único dentro de la Company, no globalmente.
        /// La URL completa: {company.Slug}.digimenu.cr/{branch.Slug}
        /// </summary>
        [Required, MaxLength(60)]
        public string Slug { get; set; } = null!;

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        public Point? Location { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }

        // ── Configuración (1:1) ───────────────────────────────────────
        public BranchLocale? Locale { get; set; }

        // ── Horarios ──────────────────────────────────────────────────
        public ICollection<BranchSchedule> Schedules { get; set; } = new List<BranchSchedule>();
        public ICollection<BranchSpecialDay> SpecialDays { get; set; } = new List<BranchSpecialDay>();

        /// <summary>
        /// Usuarios asignados a esta Branch (BranchAdmin y Staff).
        /// El CompanyAdmin no pertenece a ninguna Branch (BranchId = null en AppUser).
        /// </summary>
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();

        // ── Eventos ──────────────────────────────────────────────────
        public ICollection<BranchEvent> Events { get; set; } = new List<BranchEvent>();
    }
}
