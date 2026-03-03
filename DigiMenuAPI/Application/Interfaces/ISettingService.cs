using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface ISettingService
    {
        Task<OperationResult<SettingReadDto>> Get();
        Task<OperationResult<bool>> Update(SettingUpdateDto dto);
    }
}