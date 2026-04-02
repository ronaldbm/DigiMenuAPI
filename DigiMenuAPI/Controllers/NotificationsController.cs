using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers;

[Route("api/notifications")]
[Authorize]
public class NotificationsController : BaseController
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult> GetMine(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20)
        => HandleResult(await _service.GetMine(page, pageSize));

    [HttpGet("unread-count")]
    public async Task<ActionResult> GetUnreadCount()
        => HandleResult(await _service.GetUnreadCount());

    [HttpPatch("{id:int}/read")]
    public async Task<ActionResult> MarkAsRead(int id)
        => HandleResult(await _service.MarkAsRead(id));

    [HttpPatch("read-all")]
    public async Task<ActionResult> MarkAllAsRead()
        => HandleResult(await _service.MarkAllAsRead());
}
