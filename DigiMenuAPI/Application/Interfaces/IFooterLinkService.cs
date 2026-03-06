using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IFooterLinkService
    {
        /// <summary>
        /// Devuelve los footer links de una Branch específica.
        /// Valida que la Branch pertenece al tenant autenticado.
        /// </summary>
        Task<OperationResult<List<FooterLinkReadDto>>> GetAll(int branchId);

        Task<OperationResult<FooterLinkReadDto>> Create(FooterLinkCreateDto dto);
        Task<OperationResult<bool>> Update(FooterLinkUpdateDto dto);
        Task<OperationResult<bool>> Delete(int id);
    }
}