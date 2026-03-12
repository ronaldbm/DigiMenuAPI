using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Auth;
using DigiMenuAPI.Application.DTOs.Create;

namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>
    /// Responsabilidades exclusivas de autenticación:
    ///   - Registro de empresa + primer admin (flujo público)
    ///   - Login y generación de JWT
    ///   - Gestión de contraseña del usuario autenticado
    ///
    /// La creación de usuarios adicionales es responsabilidad de IUserService.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registra una nueva empresa con su branch principal y primer CompanyAdmin.
        /// Endpoint público — no requiere JWT.
        /// </summary>
        Task<OperationResult<LoginResponseDto>> RegisterCompany(CompanyCreateDto dto);

        /// <summary>
        /// Autentica un usuario y devuelve JWT + MustChangePassword flag.
        /// Endpoint público — no requiere JWT.
        /// </summary>
        Task<OperationResult<LoginResponseDto>> Login(LoginRequestDto dto);

        /// <summary>
        /// Cambia la contraseña del usuario autenticado.
        /// Requiere JWT — el UserId se toma del token.
        /// </summary>
        Task<OperationResult<bool>> ChangePassword(ChangePasswordDto dto);

        /// <summary>
        /// Genera token de recuperación y encola email con el link.
        /// Endpoint público — siempre devuelve Ok (nunca confirma si el email existe).
        /// </summary>
        Task<OperationResult<bool>> ForgotPassword(ForgotPasswordDto dto);

        /// <summary>
        /// Valida que el token de recuperación existe, no expiró y no fue usado.
        /// Endpoint público — el frontend llama esto al cargar la página de reset.
        /// </summary>
        Task<OperationResult<bool>> ValidateResetToken(string token);

        /// <summary>
        /// Aplica nueva contraseña usando el token del email y lo invalida.
        /// Endpoint público — no requiere JWT.
        /// </summary>
        Task<OperationResult<bool>> ResetPassword(ResetPasswordDto dto);
    }
}