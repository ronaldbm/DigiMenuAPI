using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Horario semanal de una Branch. Siempre 7 filas por Branch,
    /// una por día de la semana (0=Domingo … 6=Sábado),
    /// siguiendo la convención DayOfWeek de .NET.
    ///
    /// Se genera automáticamente al crear la Branch con defaults:
    ///   Lun-Sáb → IsOpen=true, OpenTime=09:00, CloseTime=22:00
    ///   Dom     → IsOpen=false
    ///
    /// Usos:
    ///   1. Informativo: el cliente ve el horario en el menú público.
    ///   2. Reservas: ReservationService valida contra este horario
    ///      (si el módulo RESERVATIONS está activo).
    ///
    /// Prioridad en reservas:
    ///   BranchSpecialDay (si existe para la fecha) > BranchSchedule
    /// </summary>
    public class BranchSchedule : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        /// <summary>
        /// Día de la semana. Convención .NET DayOfWeek:
        ///   0=Sunday | 1=Monday | 2=Tuesday | 3=Wednesday
        ///   4=Thursday | 5=Friday | 6=Saturday
        /// </summary>
        [Range(0, 6)]
        public byte DayOfWeek { get; set; }

        /// <summary>
        /// Indica si el local atiende este día.
        /// false → OpenTime y CloseTime son null.
        /// </summary>
        public bool IsOpen { get; set; }

        /// <summary>
        /// Hora de apertura. Null si IsOpen = false.
        /// Ejemplo: 09:00:00
        /// </summary>
        public TimeSpan? OpenTime { get; set; }

        /// <summary>
        /// Hora de cierre. Null si IsOpen = false.
        /// En reservas se combina con BranchReservationForm.MinutesBeforeClosing
        /// para calcular la última hora aceptada.
        /// Ejemplo: 22:00:00
        /// </summary>
        public TimeSpan? CloseTime { get; set; }
    }
}