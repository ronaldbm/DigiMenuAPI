using DigiMenuAPI.Application.Common.Enums;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Reportes y KPIs de cuentas.
    ///   GET /api/reports/accounts/kpis   → KPIs agregados
    ///   GET /api/reports/accounts        → Lista paginada para reportes
    ///   GET /api/reports/customers/{id}/statement → Estado de cuenta del cliente
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : BaseController
    {
        private readonly IReportService _service;

        public ReportsController(IReportService service)
        {
            _service = service;
        }

        /// <summary>KPIs de cuentas filtrados por sucursal y rango de fechas.</summary>
        [HttpGet("accounts/kpis")]
        public async Task<ActionResult> GetAccountKpis(
            [FromQuery] int? branchId = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
            => HandleResult(await _service.GetAccountKpis(branchId, from, to));

        /// <summary>Lista paginada de cuentas para tabla de reportes.</summary>
        [HttpGet("accounts")]
        public async Task<ActionResult> GetAccountReport(
            [FromQuery] int? branchId = null,
            [FromQuery] AccountStatus? status = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDesc = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => HandleResult(await _service.GetAccountReport(branchId, status, from, to, sortBy, sortDesc, page, pageSize));

        /// <summary>Datos de recibo de una cuenta para impresión.</summary>
        [HttpGet("accounts/{accountId:int}/receipt")]
        public async Task<ActionResult> GetAccountReceipt(int accountId)
            => HandleResult(await _service.GetAccountReceipt(accountId));

        /// <summary>Estado de cuenta de un cliente.</summary>
        [HttpGet("customers/{customerId:int}/statement")]
        public async Task<ActionResult> GetCustomerStatement(
            int customerId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
            => HandleResult(await _service.GetCustomerStatement(customerId, from, to));
    }
}
