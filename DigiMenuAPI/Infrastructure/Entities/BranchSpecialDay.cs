using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Día especial o feriado de una Branch que sobreescribe el horario semanal.
    ///
    /// Dos tipos:
    ///   IsClosed = true  → local completamente cerrado ese día.
    ///                       OpenTime y CloseTime son null.
    ///   IsClosed = false → local abre con horario diferente al habitual.
    ///                       OpenTime y CloseTime sobreescriben BranchSchedule.
    ///
    /// Reglas de negocio:
    ///   - No se pueden crear días con fecha pasada.
    ///   - Una Branch no puede tener dos registros para la misma fecha.
    ///   - Reason es obligatorio — documenta el motivo (feriado, evento, etc).
    ///   - Los registros se conservan indefinidamente como historial.
    ///   - No hay soft delete — eliminación física.
    ///
    /// Usos:
    ///   1. Informativo: días especiales próximos se muestran en el menú público.
    ///   2. Reservas: ReservationService los consulta con prioridad sobre BranchSchedule.
    ///
    /// Prioridad en reservas:
    ///   BranchSpecialDay (si existe para la fecha) > BranchSchedule (DayOfWeek)
    /// </summary>
    public class BranchSpecialDay : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        /// <summary>
        /// Fecha del día especial. Única por Branch.
        /// No puede ser fecha pasada al momento de creación.
        /// Se normaliza a solo fecha (sin componente de hora) al guardar.
        /// </summary>
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        /// <summary>
        /// true  → local cerrado todo el día (OpenTime y CloseTime son null).
        /// false → local abre con horario diferente (OpenTime y CloseTime requeridos).
        /// </summary>
        public bool IsClosed { get; set; }

        /// <summary>Hora de apertura especial. Null si IsClosed = true.</summary>
        public TimeSpan? OpenTime { get; set; }

        /// <summary>Hora de cierre especial. Null si IsClosed = true.</summary>
        public TimeSpan? CloseTime { get; set; }

        /// <summary>
        /// Motivo del día especial. Obligatorio.
        /// Ejemplos: "Navidad", "Feriado nacional", "Evento privado",
        ///           "Nochebuena - cierre temprano", "Mantenimiento"
        /// Se muestra al cliente en el menú público.
        /// </summary>
        [Required, MaxLength(200)]
        public string Reason { get; set; } = null!;
    }
}