using System.ComponentModel.DataAnnotations;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Suscripción activa de una Company a un Plan.
    /// El SuperAdmin gestiona el ciclo de vida: crear, actualizar estado,
    /// renovar y suspender/cancelar por falta de pago.
    ///
    /// Flujo de estados:
    ///   Trial(0)     → período de prueba gratuito
    ///   Active(1)    → suscripción vigente y al día
    ///   Suspended(2) → acceso restringido por falta de pago (gracia antes de cancelar)
    ///   Expired(3)   → fecha de vencimiento superada, sin renovación
    ///   Cancelled(4) → cancelada explícitamente por el SuperAdmin
    /// </summary>
    public class Subscription : BaseEntity
    {
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        public int PlanId { get; set; }
        public Plan Plan { get; set; } = null!;

        /// <summary>Estado actual de la suscripción. Guardado como tinyint en BD.</summary>
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;

        /// <summary>Fecha de inicio del período actual.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Fecha de vencimiento del período actual.</summary>
        public DateTime EndDate { get; set; }

        /// <summary>Próxima fecha de facturación. Null si está cancelada o sin ciclo definido.</summary>
        public DateTime? NextBillingDate { get; set; }

        /// <summary>Fecha en que vence el trial. Null si no aplica.</summary>
        public DateTime? TrialEndsAt { get; set; }

        /// <summary>Fecha en que fue suspendida. Null si nunca fue suspendida.</summary>
        public DateTime? SuspendedAt { get; set; }

        /// <summary>Motivo de la suspensión. Null si no está suspendida.</summary>
        [MaxLength(500)]
        public string? SuspendedReason { get; set; }

        /// <summary>Notas internas del SuperAdmin sobre esta suscripción.</summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        // ── Navegación ───────────────────────────────────────────────
        public ICollection<PaymentRecord> Payments { get; set; } = new List<PaymentRecord>();
    }

    /// <summary>Estado de la suscripción. Guardado como tinyint en BD.</summary>
    public enum SubscriptionStatus : byte
    {
        Trial     = 0,
        Active    = 1,
        Suspended = 2,
        Expired   = 3,
        Cancelled = 4
    }
}
