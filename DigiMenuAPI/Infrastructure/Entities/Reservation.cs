using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Reserva realizada por un cliente en una Branch específica.
    /// El módulo de reservas debe estar activo (CompanyModule) para que
    /// la Branch pueda recibir reservas.
    /// </summary>
    public class Reservation : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        // ── Datos del cliente ─────────────────────────────────────────
        [Required, MaxLength(100)]
        public string CustomerName { get; set; } = null!;

        [Required, MaxLength(20)]
        public string Phone { get; set; } = null!;

        // ── Datos de la reserva ───────────────────────────────────────
        [Required]
        [Column(TypeName = "date")]
        public DateTime ReservationDate { get; set; }

        [Required]
        public TimeSpan ReservationTime { get; set; }

        [Range(1, 100)]
        public int PeopleCount { get; set; }

        [MaxLength(20)]
        public string? TableNumber { get; set; }

        [MaxLength(500)]
        public string? Allergies { get; set; }

        [MaxLength(500)]
        public string? Comments { get; set; }

        /// <summary>1: Pending | 2: Confirmed | 3: Cancelled | 4: Completed</summary>
        public byte Status { get; set; } = 1;

        public bool IsDeleted { get; set; }
    }
}
