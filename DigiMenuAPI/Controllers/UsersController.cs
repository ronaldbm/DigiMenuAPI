using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Update;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Gestión de usuarios del tenant autenticado.
    ///
    /// GET    /api/users           → Listar usuarios
    /// GET    /api/users/{id}      → Detalle de un usuario
    /// POST   /api/users           → Crear usuario con contraseña temporal
    /// PUT    /api/users/{id}      → Editar nombre, email y Branch
    /// PATCH  /api/users/{id}/toggle-active  → Activar / desactivar
    /// DELETE /api/users/{id}      → Soft delete
    /// POST   /api/users/{id}/reset-password → Nueva contraseña temporal + email
    /// </summary>
    [Route("api/users")]
    [Authorize]
    public class UsersController : BaseController
    {
        private readonly IUserService _service;

        public UsersController(IUserService service)
        {
            _service = service;
        }

        /// <summary>Lista usuarios de la empresa. BranchAdmin solo ve su Branch.</summary>
        [HttpGet]
        public async Task<ActionResult> GetAll()
            => HandleResult(await _service.GetAll());

        /// <summary>Detalle completo de un usuario.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetById(int id)
            => HandleResult(await _service.GetById(id));

        /// <summary>
        /// Crea un usuario con contraseña temporal.
        /// El usuario recibirá un email con sus credenciales.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] AppUserCreateDto dto)
            => HandleResult(await _service.Create(dto));

        /// <summary>Edita nombre, email y Branch asignada. No modifica el rol.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] AppUserUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("El Id de la ruta no coincide con el del body.");

            return HandleResult(await _service.Update(dto));
        }

        /// <summary>
        /// Activa o desactiva un usuario.
        /// Un usuario no puede desactivarse a sí mismo.
        /// </summary>
        [HttpPatch("{id:int}/toggle-active")]
        public async Task<ActionResult> ToggleActive(int id)
            => HandleResult(await _service.ToggleActive(id));

        /// <summary>
        /// Elimina un usuario (soft delete).
        /// Un usuario no puede eliminarse a sí mismo.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
            => HandleResult(await _service.Delete(id));

        /// <summary>
        /// Genera nueva contraseña temporal y envía email al usuario.
        /// Útil cuando el usuario olvida su contraseña o es nuevo en el sistema.
        /// </summary>
        [HttpPost("{id:int}/reset-password")]
        public async Task<ActionResult> ResetPassword(int id)
            => HandleResult(await _service.ResetPassword(id));
    }
}