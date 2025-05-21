using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface ISocialLinkService
    {
        Task<OperationResult<bool>> Update(List<SocialLinkUpdateDto> socialLink);
        Task<List<SocialLinkDto>> GetAll();
    }
}
