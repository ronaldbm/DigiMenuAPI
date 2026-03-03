using DigiMenuAPI.Application.DTOs.Auth;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>Registro de nueva empresa + primer admin. Público.</summary>
        [HttpPost("register-company")]
        [AllowAnonymous]
        public async Task<ActionResult> RegisterCompany([FromBody] RegisterCompanyDto dto)
            => HandleResult(await _authService.RegisterCompany(dto));

        /// <summary>Login. Devuelve JWT con companyId, role, etc.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login([FromBody] LoginDto dto)
            => HandleResult(await _authService.Login(dto));

        /// <summary>Admin crea usuario staff dentro de su empresa.</summary>
        [HttpPost("register-user")]
        [Authorize]
        public async Task<ActionResult> RegisterUser([FromBody] RegisterUserDto dto)
            => HandleResult(await _authService.RegisterUser(dto));
    }
}