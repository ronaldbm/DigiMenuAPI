using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// CRUD de planes de suscripción.
    /// Todos los endpoints requieren role=255 (SuperAdmin).
    /// </summary>
    [Route("api/superadmin/plans")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class SuperAdminPlansController : BaseController
    {
        private readonly ISuperAdminPlanService _service;

        public SuperAdminPlansController(ISuperAdminPlanService service)
            => _service = service;

        [HttpGet]
        public async Task<ActionResult> GetAll()
            => HandleResult(await _service.GetAll());

        [HttpGet("{planId:int}")]
        public async Task<ActionResult> GetById(int planId)
            => HandleResult(await _service.GetById(planId));

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] PlanUpsertDto dto)
            => HandleResult(await _service.Create(dto));

        [HttpPut("{planId:int}")]
        public async Task<ActionResult> Update(int planId, [FromBody] PlanUpsertDto dto)
            => HandleResult(await _service.Update(planId, dto));

        [HttpPatch("{planId:int}/toggle-active")]
        public async Task<ActionResult> ToggleActive(int planId)
            => HandleResult(await _service.ToggleActive(planId));
    }
}
