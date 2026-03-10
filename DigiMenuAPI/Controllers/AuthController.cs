using DigiMenuAPI.Application.DTOs.Auth;
using DigiMenuAPI.Application.DTOs.Create;
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
        public async Task<ActionResult> RegisterCompany([FromBody] CompanyCreateDto dto)
            => HandleResult(await _authService.RegisterCompany(dto));

        /// <summary>Login. Devuelve JWT + MustChangePassword.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login([FromBody] LoginRequestDto dto)
            => HandleResult(await _authService.Login(dto));

        /// <summary>Admin crea usuario staff dentro de su empresa.</summary>
        [HttpPost("register-user")]
        [Authorize]
        public async Task<ActionResult> RegisterUser([FromBody] AppUserCreateDto dto)
            => HandleResult(await _authService.RegisterUser(dto));

        /// <summary>Cambia contraseña del usuario autenticado.</summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
            => HandleResult(await _authService.ChangePassword(dto));

        /// <summary>
        /// Solicita recuperación de contraseña.
        /// Siempre devuelve 200 — nunca confirma si el email existe.
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
            => HandleResult(await _authService.ForgotPassword(dto));

        /// <summary>
        /// Valida token de recuperación. El frontend llama esto al
        /// cargar la página de reset para verificar antes de mostrar el form.
        /// </summary>
        [HttpGet("validate-reset-token/{token}")]
        [AllowAnonymous]
        public async Task<ActionResult> ValidateResetToken(string token)
            => HandleResult(await _authService.ValidateResetToken(token));

        /// <summary>Aplica nueva contraseña usando el token del email.</summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
            => HandleResult(await _authService.ResetPassword(dto));
    }
}