using AppCore.Application.Common;
using DigiMenuAPI.Application.Common.Enums;
using DigiMenuAPI.Application.DTOs.Read;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IReportService
    {
        /// <summary>KPIs de cuentas filtrados por sucursal y rango de fechas.</summary>
        Task<OperationResult<AccountKpiDto>> GetAccountKpis(int? branchId, DateTime? from, DateTime? to);

        /// <summary>Lista paginada de cuentas para reportes con filtros.</summary>
        Task<OperationResult<PagedResult<AccountReportRowDto>>> GetAccountReport(
            int? branchId, AccountStatus? status, DateTime? from, DateTime? to,
            string? sortBy, bool sortDesc, int page, int pageSize);

        /// <summary>Estado de cuenta de un cliente en un rango de fechas.</summary>
        Task<OperationResult<CustomerStatementDto>> GetCustomerStatement(int customerId, DateTime? from, DateTime? to);

        /// <summary>Datos completos de una cuenta para generar recibo.</summary>
        Task<OperationResult<AccountReceiptDto>> GetAccountReceipt(int accountId);
    }
}
