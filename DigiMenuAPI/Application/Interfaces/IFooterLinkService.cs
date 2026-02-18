using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IFooterLinkService
    {
        Task<OperationResult<List<FooterLinkReadDto>>> GetAll();
        Task<OperationResult<FooterLinkReadDto>> Create(FooterLinkCreateDto dto);
        Task<OperationResult<bool>> Update(FooterLinkUpdateDto dto);
        Task<OperationResult<bool>> Delete(int id);
    }
}