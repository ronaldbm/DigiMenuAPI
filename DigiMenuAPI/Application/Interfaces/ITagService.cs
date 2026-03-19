using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface ITagService
    {
        /// <summary>
        /// Listado de etiquetas de la empresa autenticada.
        /// Si se pasa lang, resuelve el nombre en ese idioma (con fallback a la primera traducción disponible).
        /// </summary>
        Task<OperationResult<List<TagListItemDto>>> GetAll(string? lang = null);
        Task<OperationResult<TagReadDto>> GetById(int id);

        /// <summary>CompanyId se inyecta desde el JWT — no viene en el DTO.</summary>
        Task<OperationResult<TagReadDto>> Create(TagCreateDto dto);
        Task<OperationResult<bool>> Update(TagUpdateDto dto);
        Task<OperationResult<bool>> Delete(int id);
    }
}
