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

        /// <summary>URL del flyer promocional (subido por el administrador).</summary>
        [MaxLength(500)]
        public string? FlyerImageUrl { get; set; }

        /// <summary>Si true, se muestra un modal al abrir el menú público.</summary>
        public bool ShowPromotionalModal { get; set; }

        /// <summary>Permite ocultar temporalmente el evento sin eliminarlo.</summary>
        public bool IsActive { get; set; } = true;
    }
}
