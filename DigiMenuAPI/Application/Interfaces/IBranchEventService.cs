using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>
    /// Gestión de eventos promocionales de una sucursal.
    /// </summary>
    public interface IBranchEventService
    {
        /// <summary>Todos los eventos de la sucursal (admin). Incluye pasados.</summary>
        Task<OperationResult<List<BranchEventReadDto>>> GetEvents(int branchId);

        /// <summary>Eventos próximos visibles públicamente (hoy en adelante, IsActive=true).</summary>
        Task<OperationResult<List<BranchEventReadDto>>> GetUpcomingEvents(string companySlug, string branchSlug);

        /// <summary>
        /// El evento activo con ShowPromotionalModal=true más próximo a hoy.
        /// Devuelve null en data si no hay ninguno. Usado por el anuncio de bienvenida del menú.
        /// </summary>
        Task<OperationResult<BranchEventReadDto?>> GetNextAnnouncement(string companySlug, string branchSlug);

        /// <summary>Un evento por Id (admin).</summary>
        Task<OperationResult<BranchEventReadDto>> GetById(int id);

        /// <summary>Crea un evento con flyer opcional.</summary>
        Task<OperationResult<BranchEventReadDto>> Create(BranchEventCreateDto dto);

        /// <summary>Actualiza un evento; si se sube nuevo flyer, reemplaza el anterior.</summary>
        Task<OperationResult<BranchEventReadDto>> Update(BranchEventUpdateDto dto);

        /// <summary>Elimina el evento y su flyer asociado.</summary>
        Task<OperationResult<bool>> Delete(int id);
    }
}
