using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Auth;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IAuthService
    {
        Task<OperationResult<AuthResultDto>> RegisterCompany(RegisterCompanyDto dto);
        Task<OperationResult<AuthResultDto>> Login(LoginDto dto);
        Task<OperationResult<bool>> RegisterUser(RegisterUserDto dto);
    }
}