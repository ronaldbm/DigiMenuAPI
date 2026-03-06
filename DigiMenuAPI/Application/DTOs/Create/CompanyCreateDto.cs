namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// El SuperAdmin crea una Company asignándole un Plan.
    /// MaxBranches y MaxUsers se inicializan con los valores del Plan seleccionado
    /// pero pueden ajustarse de forma puntual si es necesario.
    /// </summary>
    public record CompanyCreateDto(
        string Name,
        string Email,
        string? Phone,
        string? CountryCode,
        int PlanId,
        int? MaxBranches,   // null = usar el valor del Plan
        int? MaxUsers,      // null = usar el valor del Plan
        string Slug
    );
}
