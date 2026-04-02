using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers;

[Route("api/branch-discounts")]
[Authorize]
public class BranchDiscountsController : BaseController
{
    private readonly IBranchDiscountService _service;

    public BranchDiscountsController(IBranchDiscountService service) => _service = service;

    [HttpGet("{branchId:int}")]
    public async Task<ActionResult> GetByBranch(int branchId)
        => HandleResult(await _service.GetByBranch(branchId));

    [HttpGet("item/{id:int}")]
    public async Task<ActionResult> GetById(int id)
        => HandleResult(await _service.GetById(id));

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] BranchDiscountCreateDto dto)
        => HandleResult(await _service.Create(dto));

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] BranchDiscountUpdateDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new { Success = false, Message = "El Id de la ruta no coincide con el del body." });

        return HandleResult(await _service.Update(dto));
    }

    [HttpPatch("{id:int}/toggle")]
    public async Task<ActionResult> ToggleActive(int id)
        => HandleResult(await _service.ToggleActive(id));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
        => HandleResult(await _service.Delete(id));
}
