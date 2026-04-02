using System.Globalization;
using DigiMenuAPI.Application.Common.Enums;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Controllers;

[Route("api/accounts")]
[Authorize]
public class AccountsController : BaseController
{
    private readonly IAccountService      _service;
    private readonly IAccountAuditService _auditService;
    private readonly ApplicationDbContext _context;

    public AccountsController(
        IAccountService service,
        IAccountAuditService auditService,
        ApplicationDbContext context)
    {
        _service      = service;
        _auditService = auditService;
        _context      = context;
    }

    [HttpGet("{branchId:int}")]
    public async Task<ActionResult> GetByBranch(
        int branchId,
        [FromQuery] AccountStatus? status = null,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20)
        => HandleResult(await _service.GetByBranch(branchId, status, page, pageSize));

    [HttpGet("detail/{id:int}")]
    public async Task<ActionResult> GetById(int id)
        => HandleResult(await _service.GetById(id));

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] AccountCreateDto dto)
        => HandleResult(await _service.Create(dto));

    [HttpPost("{id:int}/items")]
    public async Task<ActionResult> AddItem(int id, [FromBody] AccountItemCreateDto dto)
    {
        if (id != dto.AccountId)
            return BadRequest(new { Success = false, Message = "El Id de la ruta no coincide con el del body." });

        var conflict = await CheckConflict(dto.AccountId);
        if (conflict != null) return conflict;

        return HandleResult(await _service.AddItem(dto));
    }

    [HttpPut("items/{itemId:int}")]
    public async Task<ActionResult> UpdateItem(int itemId, [FromBody] AccountItemUpdateDto dto)
    {
        if (itemId != dto.Id)
            return BadRequest(new { Success = false, Message = "El Id de la ruta no coincide con el del body." });

        var conflict = await CheckConflict(dto.AccountId);
        if (conflict != null) return conflict;

        return HandleResult(await _service.UpdateItem(dto));
    }

    [HttpDelete("items/{itemId:int}")]
    public async Task<ActionResult> RemoveItem(int itemId)
    {
        var accountId = await _context.AccountItems
            .Where(i => i.Id == itemId)
            .Select(i => i.AccountId)
            .FirstOrDefaultAsync();

        if (accountId > 0)
        {
            var conflict = await CheckConflict(accountId);
            if (conflict != null) return conflict;
        }

        return HandleResult(await _service.RemoveItem(itemId));
    }

    [HttpPost("{id:int}/discounts")]
    public async Task<ActionResult> ApplyDiscount(int id, [FromBody] ApplyDiscountDto dto)
    {
        if (id != dto.AccountId)
            return BadRequest(new { Success = false, Message = "El Id de la ruta no coincide con el del body." });

        var conflict = await CheckConflict(dto.AccountId);
        if (conflict != null) return conflict;

        return HandleResult(await _service.ApplyDiscount(dto));
    }

    [HttpPatch("discounts/{discountId:int}/authorize")]
    public async Task<ActionResult> AuthorizeDiscount(int discountId, [FromBody] AuthorizeDiscountDto dto)
    {
        if (discountId != dto.AccountDiscountId)
            return BadRequest(new { Success = false, Message = "El Id de la ruta no coincide con el del body." });

        return HandleResult(await _service.AuthorizeDiscount(dto));
    }

    [HttpDelete("discounts/{discountId:int}")]
    public async Task<ActionResult> RemoveDiscount(int discountId)
        => HandleResult(await _service.RemoveDiscount(discountId));

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> SetStatus(int id, [FromBody] AccountStatusUpdateDto dto)
    {
        if (id != dto.AccountId)
            return BadRequest(new { Success = false, Message = "El Id de la ruta no coincide con el del body." });

        var conflict = await CheckConflict(dto.AccountId);
        if (conflict != null) return conflict;

        return HandleResult(await _service.SetStatus(dto));
    }

    [HttpPost("{id:int}/splits")]
    public async Task<ActionResult> CreateSplit(int id, [FromBody] AccountSplitCreateDto dto)
    {
        if (id != dto.AccountId)
            return BadRequest(new { Success = false, Message = "El Id de la ruta no coincide con el del body." });

        return HandleResult(await _service.CreateSplit(dto));
    }

    [HttpDelete("splits/{splitId:int}")]
    public async Task<ActionResult> RemoveSplit(int splitId)
        => HandleResult(await _service.RemoveSplit(splitId));

    [HttpGet("{id:int}/audit")]
    public async Task<ActionResult> GetAudit(
        int id,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20)
        => HandleResult(await _auditService.GetByAccount(id, page, pageSize));

    // ── Conflict detection ──────────────────────────────────────────

    /// <summary>
    /// If the client sends X-Expected-Modified-At, compare it with the entity's
    /// actual ModifiedAt. If the entity has been modified since, return 409 Conflict.
    /// Returns null if no conflict (or no header present).
    /// </summary>
    private async Task<ActionResult?> CheckConflict(int accountId)
    {
        var headerValue = Request.Headers["X-Expected-Modified-At"].FirstOrDefault();
        if (string.IsNullOrEmpty(headerValue)) return null;

        if (!DateTime.TryParse(headerValue, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var expectedModifiedAt))
            return null;

        var account = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(a => new { a.ModifiedAt, a.CreatedAt })
            .FirstOrDefaultAsync();

        if (account == null) return null;

        var actualModifiedAt = account.ModifiedAt ?? account.CreatedAt;

        // Allow 1 second tolerance for clock skew
        if (actualModifiedAt > expectedModifiedAt.AddSeconds(1))
        {
            return Conflict(new
            {
                Success = false,
                ErrorCode = "Conflict",
                ErrorKey = "account.conflict",
                Message = "La cuenta fue modificada por otro usuario después de tu último cambio.",
                ServerModifiedAt = actualModifiedAt,
            });
        }

        return null;
    }
}
