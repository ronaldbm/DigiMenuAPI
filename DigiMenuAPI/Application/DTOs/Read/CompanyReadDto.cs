namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// Vista completa de una Company para el panel del SuperAdmin.
    /// Incluye los módulos activos para saber qué funcionalidades tiene contratadas.
    /// </summary>
    public record CompanyReadDto(
        int Id,
        string Name,
        string Email,
        string? Phone,
        string? CountryCode,
        bool IsActive,
        // Plan y límites
        int PlanId,
        string PlanName,
        int MaxBranches,
        int MaxUsers,
        int CurrentBranches,          // calculado en el servicio con Count()
        int CurrentUsers,             // calculado en el servicio con Count()
                                      // Módulos contratados
        List<CompanyModuleReadDto> ActiveModules,
        DateTime CreatedAt
    );

    /// <summary>
    /// Vista reducida para listas, tablas y selects.
    /// No incluye módulos para mantener las consultas ligeras.
    /// </summary>
    public record CompanySummaryDto(
        int Id,
        string Name,
        string Email,
        bool IsActive,
        string PlanName,
        int CurrentBranches,
        int CurrentUsers,
        int MaxBranches,
        int MaxUsers
    );
}