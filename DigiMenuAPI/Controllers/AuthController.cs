using DigiMenuAPI.Application.DTOs.Auth;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DigiMenuAPI.Controllers
{
    /// <summary>
    /// Endpoints de autenticación y gestión de contraseña propia.
    ///
    /// Públicos (sin JWT):
    ///   POST /api/auth/register-company
    ///   POST /api/auth/login
    ///   POST /api/auth/forgot-password
    ///   GET  /api/auth/validate-reset-token/{token}
    ///   POST /api/auth/reset-password
    ///
    /// Autenticados (requieren JWT):
    ///   POST /api/auth/change-password
    ///
    /// Creación de usuarios adicionales → POST /api/users
    /// </summary>
    [Route("api/auth")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registra una nueva empresa con su branch principal y primer CompanyAdmin.
        /// Devuelve JWT listo para usar.
        /// </summary>
        [HttpPost("register-company")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult> RegisterCompany([FromBody] CompanyCreateDto dto)
            => HandleResult(await _authService.RegisterCompany(dto));

        /// <summary>Autentica un usuario. Devuelve JWT + MustChangePassword flag.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult> Login([FromBody] LoginRequestDto dto)
            => HandleResult(await _authService.Login(dto));

        /// <summary>
        /// Cambia la contraseña del usuario autenticado.
        /// Requiere la contraseña actual para confirmar identidad.
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
            => HandleResult(await _authService.ChangePassword(dto));

        /// <summary>
        /// Solicita recuperación de contraseña por email.
        /// Siempre devuelve 200 — nunca confirma si el email existe.
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
            => HandleResult(await _authService.ForgotPassword(dto));

        /// <summary>
        /// Valida token de recuperación antes de mostrar el formulario de reset.
        /// </summary>
        [HttpGet("validate-reset-token/{token}")]
        [AllowAnonymous]
        public async Task<ActionResult> ValidateResetToken(string token)
            => HandleResult(await _authService.ValidateResetToken(token));

        /// <summary>Aplica nueva contraseña usando el token recibido por email.</summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
            => HandleResult(await _authService.ResetPassword(dto));
    }
}