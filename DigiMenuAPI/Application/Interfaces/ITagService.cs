using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface ITagService
    {
        /// <summary>Catálogo global de etiquetas de la empresa autenticada. CompanyId desde JWT.</summary>
        Task<OperationResult<List<TagReadDto>>> GetAll();
        Task<OperationResult<TagReadDto>> GetById(int id);

        /// <summary>CompanyId se inyecta desde el JWT — no viene en el DTO.</summary>
        Task<OperationResult<TagReadDto>> Create(TagCreateDto dto);
        Task<OperationResult<bool>> Update(TagUpdateDto dto);
        Task<OperationResult<bool>> Delete(int id);
    }
}