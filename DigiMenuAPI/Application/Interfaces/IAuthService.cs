using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Auth;
using DigiMenuAPI.Application.DTOs.Create;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IAuthService
    {
        Task<OperationResult<LoginResponseDto>> RegisterCompany(CompanyCreateDto dto);
        Task<OperationResult<LoginResponseDto>> Login(LoginRequestDto dto);
        Task<OperationResult<bool>> RegisterUser(AppUserCreateDto dto);
        Task<OperationResult<bool>> ChangePassword(ChangePasswordDto dto);

        /// <summary>
        /// Genera token de recuperación y encola email con el link.
        /// Siempre devuelve Ok — nunca confirma si el email existe (seguridad).
        /// </summary>
        Task<OperationResult<bool>> ForgotPassword(ForgotPasswordDto dto);

        /// <summary>Valida que el token existe, no expiró y no fue usado.</summary>
        Task<OperationResult<bool>> ValidateResetToken(string token);

        /// <summary>Aplica la nueva contraseña y marca el token como usado.</summary>
        Task<OperationResult<bool>> ResetPassword(ResetPasswordDto dto);
    }
}