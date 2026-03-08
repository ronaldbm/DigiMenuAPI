namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Configuración del formulario público de reservas de una Branch.
    /// Define qué campos se muestran y cuáles son obligatorios.
    ///
    /// IMPORTANTE: Esta entidad solo existe si la Branch tiene activo
    /// el módulo RESERVATIONS. El servicio verifica el módulo antes
    /// de cualquier lectura o escritura.
    ///
    /// Regla de negocio: un campo no puede ser requerido si no se muestra.
    /// Esta validación se aplica en el servicio, no en la entidad.
    ///
    /// Relación 1:1 con Branch.
    /// </summary>
    public class BranchReservationForm : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

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