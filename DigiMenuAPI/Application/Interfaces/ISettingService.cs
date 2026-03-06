using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface ISettingService
    {
        /// <summary>
        /// Devuelve la configuración de una Branch específica.
        /// Valida que la Branch pertenece a la empresa del usuario autenticado.
        /// </summary>
        Task<OperationResult<SettingReadDto>> Get(int branchId);

        /// <summary>
        /// Actualiza la configuración de la Branch indicada en el DTO.
        /// Valida ownership antes de actualizar.
        /// </summary>
        Task<OperationResult<bool>> Update(SettingUpdateDto dto);
    }
}