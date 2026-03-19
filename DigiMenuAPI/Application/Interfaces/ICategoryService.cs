using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface ICategoryService
    {
        /// <summary>
        /// Listado del catálogo de la empresa autenticada.
        /// Si se pasa lang, resuelve el nombre en ese idioma (con fallback a la primera traducción disponible).
        /// </summary>
        Task<OperationResult<List<CategoryListItemDto>>> GetAll(string? lang = null);
        Task<OperationResult<CategoryReadDto>> GetById(int id);

        /// <summary>CompanyId se inyecta desde el JWT — no viene en el DTO.</summary>
        Task<OperationResult<CategoryReadDto>> Create(CategoryCreateDto dto);
        Task<OperationResult<bool>> Update(CategoryUpdateDto dto);
        Task<OperationResult<bool>> Delete(int id);

        /// <summary>Reordena varias categorías en una sola operación.</summary>
        Task<OperationResult<bool>> Reorder(List<ReorderItemDto> items);
    }
}
