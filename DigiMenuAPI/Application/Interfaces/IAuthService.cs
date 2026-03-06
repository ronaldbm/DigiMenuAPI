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
    }
}