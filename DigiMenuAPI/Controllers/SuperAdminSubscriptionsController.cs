using AppCore.Domain.Entities;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Gestión de suscripciones y pagos desde el panel SuperAdmin.
    /// Todos los endpoints requieren role=255 (SuperAdmin).
    /// </summary>
    [Route("api/superadmin")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class SuperAdminSubscriptionsController : BaseController
    {
        private readonly ISuperAdminSubscriptionService _service;

        public SuperAdminSubscriptionsController(ISuperAdminSubscriptionService service)
            => _service = service;

        // ── Suscripciones ─────────────────────────────────────────────

        /// <summary>Lista todas las suscripciones. Filtra por estado con ?status=Active|Trial|etc.</summary>
        [HttpGet("subscriptions")]
        public async Task<ActionResult> GetAll([FromQuery] string? status)
            => HandleResult(await _service.GetAll(status));

        /// <summary>Suscripción de un tenant específico.</summary>
        [HttpGet("companies/{companyId:int}/subscription")]
        public async Task<ActionResult> GetByCompany(int companyId)
            => HandleResult(await _service.GetByCompany(companyId));

        /// <summary>Tenants cuya suscripción vence pronto.</summary>
        [HttpGet("subscriptions/expiring-soon")]
        public async Task<ActionResult> GetExpiringSoon([FromQuery] int days = 30)
            => HandleResult(await _service.GetExpiringSoon(days));

        /// <summary>Tenants con suscripción vencida o suspendida (at-risk).</summary>
        [HttpGet("subscriptions/at-risk")]
        public async Task<ActionResult> GetAtRisk()
            => HandleResult(await _service.GetAtRisk());

        /// <summary>Actualiza estado, fechas y notas de la suscripción de un tenant.</summary>
        [HttpPatch("companies/{companyId:int}/subscription")]
        public async Task<ActionResult> Update(
            int companyId, [FromBody] UpdateSubscriptionDto dto)
            => HandleResult(await _service.Update(companyId, dto));

        // ── Pagos ─────────────────────────────────────────────────────

        /// <summary>Historial de pagos de un tenant.</summary>
        [HttpGet("companies/{companyId:int}/payments")]
        public async Task<ActionResult> GetPayments(int companyId)
            => HandleResult(await _service.GetPayments(companyId));

        /// <summary>Registra un pago manual recibido de un tenant.</summary>
        [HttpPost("companies/{companyId:int}/payments")]
        public async Task<ActionResult> RegisterPayment(
            int companyId, [FromBody] RegisterPaymentDto dto)
            => HandleResult(await _service.RegisterPayment(companyId, dto));

        /// <summary>Actualiza el estado de un pago (ej. marcar como Refunded).</summary>
        [HttpPatch("payments/{paymentId:int}/status")]
        public async Task<ActionResult> UpdatePaymentStatus(
            int paymentId, [FromBody] PaymentStatus status)
            => HandleResult(await _service.UpdatePaymentStatus(paymentId, status));
    }
}
