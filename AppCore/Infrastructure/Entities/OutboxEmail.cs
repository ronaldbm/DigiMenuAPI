using System.ComponentModel.DataAnnotations;
using AppCore.Domain.Entities;

namespace AppCore.Infrastructure.Entities
{
    /// <summary>
    /// Registro liviano de correos electrónicos en cola.
    /// Implementa el Outbox Pattern para garantizar entrega confiable.
    ///
    /// HtmlBody vive en OutboxEmailBody (relación 1:1) para que los
    /// queries del processor sean livianos — nunca cargan el HTML
    /// hasta el momento exacto del envío.
    ///
    /// Flujo de estados:
    ///   Pending(0)   → encolado, aún no intentado
    ///   Sent(1)      → enviado exitosamente
    ///   Failed(2)    → último intento falló, puede reintentarse
    ///   Abandoned(3) → superó MaxRetries, no se reintentará
    ///
    /// Backoff exponencial entre reintentos:
    ///   Intento 1 → 1 min | 2 → 2 min | 3 → 4 min | N → 2^(N-1) min
    /// </summary>
    public class OutboxEmail : BaseEntity
    {
        // ── Multi-Tenant ──────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        /// <summary>Sucursal origen. Null para correos a nivel empresa.</summary>
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        // ── Destinatario ──────────────────────────────────────────────
        [Required, MaxLength(150)]
        public string ToEmail { get; set; } = null!;

        [Required, MaxLength(150)]
        public string ToName { get; set; } = null!;

        [Required, MaxLength(300)]
        public string Subject { get; set; } = null!;

        // ── Clasificación ─────────────────────────────────────────────
        /// <summary>
        /// Tipo de correo. Guardado como tinyint en BD.
        /// 0=Welcome | 1=TemporaryPassword | 2=ForgotPassword | 3=ReservationConfirmation
        /// </summary>
        public OutboxEmailType EmailType { get; set; }

        // ── Estado y reintentos ───────────────────────────────────────
        /// <summary>
        /// Estado actual. Guardado como tinyint en BD.
        /// 0=Pending | 1=Sent | 2=Failed | 3=Abandoned
        /// </summary>
        public OutboxEmailStatus Status { get; set; } = OutboxEmailStatus.Pending;

        public int RetryCount { get; set; }

        /// <summary>Último mensaje de error. Null si nunca falló.</summary>
        [MaxLength(2000)]
        public string? LastError { get; set; }

        /// <summary>Fecha del último intento. Null si nunca se procesó.</summary>
        public DateTime? LastAttemptAt { get; set; }

        /// <summary>Fecha de envío exitoso. Null si aún no se envió.</summary>
        public DateTime? SentAt { get; set; }

        /// <summary>
        /// Fecha mínima para el próximo intento (backoff exponencial).
        /// Null = puede procesarse de inmediato.
        /// </summary>
        public DateTime? NextRetryAt { get; set; }

        // ── Relación 1:1 con el cuerpo ────────────────────────────────
        /// <summary>
        /// Cuerpo HTML del correo. Se carga solo cuando se va a enviar.
        /// Nunca incluir en queries del processor sin necesidad.
        /// </summary>
        public OutboxEmailBody? Body { get; set; }
    }

    /// <summary>Estado del correo en la cola. Guardado como tinyint en BD.</summary>
    public enum OutboxEmailStatus : byte
    {
        Pending = 0,
        Sent = 1,
        Failed = 2,
        Abandoned = 3
    }

    /// <summary>
    /// Tipo de correo. Guardado como tinyint en BD.
    /// Al agregar un nuevo tipo, agregar aquí y en IEmailQueueService.
    /// </summary>
    public enum OutboxEmailType : byte
    {
        Welcome = 0,
        TemporaryPassword = 1,
        ForgotPassword = 2,
        ReservationConfirmation = 3
    }
}
