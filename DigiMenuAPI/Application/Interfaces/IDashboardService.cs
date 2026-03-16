using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IDashboardService
    {
        /// <summary>Devuelve los conteos del catálogo de la empresa en una sola consulta.</summary>
        Task<OperationResult<DashboardStatsDto>> GetStats();
    }
}
