using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Gestión de tenants (Companies) desde el panel SuperAdmin.
    /// Todos los endpoints requieren role=255 (SuperAdmin).
    /// La empresa maestra (Id=1) está excluida de todos los listados y operaciones.
    /// </summary>
    [Route("api/superadmin/companies")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class SuperAdminCompaniesController : BaseController
    {
        private readonly ISuperAdminCompanyService _service;

        public SuperAdminCompaniesController(ISuperAdminCompanyService service)
            => _service = service;

        /// <summary>
        /// Lista todos los tenants con filtros y paginación.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int? planId,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
            => HandleResult(await _service.GetAll(search, planId, status, page, pageSize));

        /// <summary>Vista completa de un tenant: datos, suscripción, pagos, branches, usuarios.</summary>
        [HttpGet("{companyId:int}")]
        public async Task<ActionResult> GetById(int companyId)
            => HandleResult(await _service.GetById(companyId));

        /// <summary>
        /// Da de alta un nuevo tenant.
        /// Crea Company + Subscription + CompanyAdmin (contraseña temporal por email).
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateTenantDto dto)
            => HandleResult(await _service.Create(dto));

        /// <summary>Actualiza nombre, slug, email, teléfono y estado de un tenant.</summary>
        [HttpPatch("{companyId:int}/info")]
        public async Task<ActionResult> UpdateInfo(
            int companyId, [FromBody] UpdateCompanyInfoDto dto)
            => HandleResult(await _service.UpdateInfo(companyId, dto));

        /// <summary>Cambia el plan asignado al tenant.</summary>
        [HttpPatch("{companyId:int}/plan")]
        public async Task<ActionResult> UpdatePlan(
            int companyId, [FromBody] UpdateCompanyPlanDto dto)
            => HandleResult(await _service.UpdatePlan(companyId, dto));

        /// <summary>Modifica los límites personalizados de branches y usuarios.</summary>
        [HttpPatch("{companyId:int}/limits")]
        public async Task<ActionResult> UpdateLimits(
            int companyId, [FromBody] UpdateCompanyLimitsDto dto)
            => HandleResult(await _service.UpdateLimits(companyId, dto));

        /// <summary>Activa o desactiva un tenant.</summary>
        [HttpPatch("{companyId:int}/toggle-active")]
        public async Task<ActionResult> ToggleActive(int companyId)
            => HandleResult(await _service.ToggleActive(companyId));
    }
}
