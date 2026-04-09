using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;

namespace DigiMenuAPI.Application.Interfaces
{
    public interface IBulkImportService
    {
        Task<OperationResult<CsvTemplateDto>> GetCategoryTemplate();
        Task<OperationResult<CsvTemplateDto>> GetProductTemplate();
        Task<OperationResult<CsvTemplateDto>> GetBranchProductTemplate();

        Task<OperationResult<BulkImportResultDto>> ImportCategories(BulkCategoryImportDto dto);
        Task<OperationResult<BulkImportResultDto>> ImportProducts(BulkProductImportDto dto, IFormFile? imagesZip);
        Task<OperationResult<BulkImportResultDto>> ImportBranchProducts(BulkBranchProductImportDto dto, IFormFile? imagesZip);
    }
}
