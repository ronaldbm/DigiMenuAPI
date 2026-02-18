using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [Route("api/[controller]")]
    public class ReservationsController : BaseController
    {
        private readonly IReservationService _service;

        public ReservationsController(IReservationService service)
        {
            _service = service;
        }

        [HttpGet] // Para el panel Admin
        public async Task<ActionResult> GetAll() => HandleResult(await _service.GetAll());

        [HttpPost] // Para el cliente en el Menú Público
        public async Task<ActionResult> Create([FromBody] ReservationCreateDto dto) => HandleResult(await _service.Create(dto));

        [HttpPatch("{id}/status")] // Para que el admin confirme o cancele
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] byte status) => HandleResult(await _service.UpdateStatus(id, status));

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id) => HandleResult(await _service.Delete(id));
    }
}
