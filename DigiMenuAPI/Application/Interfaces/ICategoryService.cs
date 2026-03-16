using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface ICategoryService
    {
        /// <summary>Catálogo global de la empresa autenticada. CompanyId desde JWT.</summary>
        Task<OperationResult<List<CategoryReadDto>>> GetAll();
        Task<OperationResult<CategoryReadDto>> GetById(int id);

        /// <summary>CompanyId se inyecta desde el JWT — no viene en el DTO.</summary>
        Task<OperationResult<CategoryReadDto>> Create(CategoryCreateDto dto);
        Task<OperationResult<bool>> Update(CategoryUpdateDto dto);
        Task<OperationResult<bool>> Delete(int id);

        /// <summary>Reordena varias categorías en una sola operación.</summary>
        Task<OperationResult<bool>> Reorder(List<ReorderItemDto> items);
    }
}