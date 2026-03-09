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

        /// <summary>
        /// Cambia la contraseña del usuario autenticado.
        /// Requiere la contraseña actual para confirmar identidad.
        /// Al completarse, MustChangePassword queda en false.
        /// </summary>
        Task<OperationResult<bool>> ChangePassword(ChangePasswordDto dto);
    }
}