using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Registro de un pago recibido de un tenant.
    /// El SuperAdmin lo crea manualmente al confirmar un pago (transferencia, efectivo, etc.).
    ///
    /// Flujo de estados:
    ///   Pending(0)  → registrado pero aún no confirmado
    ///   Paid(1)     → confirmado y aplicado
    ///   Failed(2)   → intento de cobro fallido (uso futuro con pasarela de pago)
    ///   Refunded(3) → devuelto al cliente
    /// </summary>
    public class PaymentRecord : BaseEntity
    {
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        /// <summary>FK a la suscripción a la que aplica este pago.</summary>
        public int SubscriptionId { get; set; }
        public Subscription Subscription { get; set; } = null!;

        // ── Datos del pago ────────────────────────────────────────────
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>Código ISO 4217. Ejemplo: "USD", "CRC".</summary>
        [Required, MaxLength(5)]
        public string Currency { get; set; } = "USD";

        /// <summary>Fecha real del pago (no necesariamente la fecha de registro).</summary>
        public DateTime PaidAt { get; set; }

        /// <summary>Método de pago. Guardado como tinyint en BD.</summary>
        public PaymentMethod Method { get; set; }

        /// <summary>Número de referencia del pago (transferencia, número de voucher, etc.).</summary>
        [MaxLength(100)]
        public string? Reference { get; set; }

        /// <summary>Notas internas del SuperAdmin sobre este pago.</summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>Estado del pago. Guardado como tinyint en BD.</summary>
        public PaymentStatus Status { get; set; } = PaymentStatus.Paid;

        /// <summary>Id del SuperAdmin que registró el pago.</summary>
        public int RecordedByUserId { get; set; }
        public AppUser RecordedBy { get; set; } = null!;
    }

    /// <summary>Estado del pago. Guardado como tinyint en BD.</summary>
    public enum PaymentStatus : byte
    {
        Pending  = 0,
        Paid     = 1,
        Failed   = 2,
        Refunded = 3
    }

    /// <summary>
    /// Método de pago. Guardado como tinyint en BD.
    /// Los valores son estables — no renombrar una vez en producción.
    /// </summary>
    public enum PaymentMethod : byte
    {
        Cash         = 0,
        BankTransfer = 1,
        Card         = 2,
        Sinpe        = 3,
        Other        = 4
    }
}
