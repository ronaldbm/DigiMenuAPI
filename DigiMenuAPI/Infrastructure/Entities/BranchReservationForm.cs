namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Configuración del formulario público de reservas de una Branch.
    ///
    /// Responsabilidades:
    ///   - Qué campos se muestran y cuáles son obligatorios en el formulario público.
    ///   - MaxCapacity: límite de personas simultáneas en el local.
    ///   - MinutesBeforeClosing: margen global antes del cierre para cortar reservas.
    ///
    /// El horario de apertura/cierre por día vive en BranchSchedule.
    /// Los días especiales/feriados viven en BranchSpecialDay.
    ///
    /// IMPORTANTE: Solo existe si la Branch tiene activo el módulo RESERVATIONS.
    ///
    /// Reglas de negocio:
    ///   - Un campo no puede ser requerido si no está visible.
    ///   - MaxCapacity = 0 significa sin límite de capacidad.
    ///   - MinutesBeforeClosing = 0 permite reservar hasta el momento exacto del cierre.
    ///
    /// Relación 1:1 con Branch.
    /// </summary>
    public class BranchReservationForm : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        // ── Capacidad y corte de reservas ─────────────────────────────

        /// <summary>
        /// Máximo de personas que pueden tener reserva activa (Pending + Confirmed)
        /// en la misma fecha simultáneamente.
        /// 0 = sin límite configurado (no se valida capacidad).
        /// </summary>
        public int MaxCapacity { get; set; } = 0;

        /// <summary>
        /// Minutos antes del cierre diario en que se deja de aceptar reservas.
        /// Aplica tanto al horario semanal como a días especiales con horario diferente.
        ///
        /// Ejemplo: CloseTime = 22:00 y MinutesBeforeClosing = 30
        ///   → última reserva aceptada: 21:30
        ///
        /// 0 = se acepta hasta el momento exacto del cierre.
        /// </summary>
        public int MinutesBeforeClosing { get; set; } = 0;

        /// <summary>
        /// Máximo de personas que se pueden por cada reserva
        /// 0 = sin límite configurado. Debe ser menor a la cantidad de personas que permite reservar el local (MaxCapacity) para evitar inconsistencias.
        /// </summary>
        public int MaxCapacityPerReservation { get; set; } = 0;


        // ── Nombre del cliente ────────────────────────────────────────
        public bool FormShowName { get; set; } = true;
        public bool FormRequireName { get; set; } = true;

        // ── Teléfono ──────────────────────────────────────────────────
        public bool FormShowPhone { get; set; } = true;
        public bool FormRequirePhone { get; set; } = true;

        // ── Mesa ─────────────────────────────────────────────────────
        public bool FormShowTable { get; set; }
        public bool FormRequireTable { get; set; }

        // ── Número de personas ────────────────────────────────────────
        public bool FormShowPersons { get; set; } = true;
        public bool FormRequirePersons { get; set; } = true;

        // ── Alergias ──────────────────────────────────────────────────
        public bool FormShowAllergies { get; set; }
        public bool FormRequireAllergies { get; set; }

        // ── Cumpleaños ────────────────────────────────────────────────
        public bool FormShowBirthday { get; set; }
        public bool FormRequireBirthday { get; set; }

        // ── Comentarios ───────────────────────────────────────────────
        public bool FormShowComments { get; set; } = true;
        public bool FormRequireComments { get; set; }
    }
}