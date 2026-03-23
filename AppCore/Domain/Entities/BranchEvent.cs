using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AppCore.Infrastructure.Entities;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Evento promocional de una sucursal.
    /// Puede tener un flyer (imagen), descripción, horario y opcionalmente
    /// mostrarse como modal al abrir el menú público.
    /// </summary>
    public class BranchEvent : BaseEntity
    {
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>Fecha del evento (sin hora).</summary>
        [Column(TypeName = "date")]
        public DateTime EventDate { get; set; }

        /// <summary>Hora de inicio del evento. Null = todo el día.</summary>
        public TimeSpan? StartTime { get; set; }

        /// <summary>Hora de fin del evento. Null = todo el día.</summary>
        public TimeSpan? EndTime { get; set; }

        /// <summary>
        /// Fecha de fin del evento. Igual a EventDate para eventos de un solo día.
        /// Automáticamente se establece como EventDate + 1 día cuando EndTime &lt; StartTime
        /// (evento de medianoche, por ej. 20:00 – 02:00).
        /// </summary>
        [Column(TypeName = "date")]
        public DateTime EndDate { get; set; }

        /// <summary>URL del flyer promocional (subido por el administrador).</summary>
        [MaxLength(500)]
        public string? FlyerImageUrl { get; set; }

        /// <summary>Si true, se muestra un modal al abrir el menú público.</summary>
        public bool ShowPromotionalModal { get; set; }

        /// <summary>Permite ocultar temporalmente el evento sin eliminarlo.</summary>
        public bool IsActive { get; set; } = true;

        // ── Metadatos de encuadre de imagen ──────────────────────────
        [MaxLength(20)] public string FlyerObjectFit { get; set; } = "cover";
        [MaxLength(50)] public string FlyerObjectPosition { get; set; } = "50% 50%";
    }
}
