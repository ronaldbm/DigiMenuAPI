using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.AddDTOs;
using DigiMenuAPI.Application.DTOs.ReadDTOs;
using DigiMenuAPI.Application.DTOs.UpdateDTOs;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface ITagService
    {
        Task<OperationResult<List<TagReadDto>>> GetAll();
        Task<OperationResult<TagReadDto>> GetById(int id);
        Task<OperationResult<TagReadDto>> Create(TagCreateDto dto);
        Task<OperationResult<bool>> Update(TagUpdateDto dto);
        Task<OperationResult<bool>> Delete(int id);
    }
}