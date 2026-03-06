using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

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