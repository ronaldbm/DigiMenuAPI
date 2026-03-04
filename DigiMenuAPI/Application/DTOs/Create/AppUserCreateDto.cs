namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// El rol máximo que se puede asignar depende del rol del creador:
    ///   SuperAdmin    → puede crear Role 1 (CompanyAdmin)
    ///   CompanyAdmin  → puede crear Role 2 (BranchAdmin)
    ///   BranchAdmin   → puede crear Role 3 (Staff)
    /// Esta validación se hace en el servicio.
    /// </summary>
    public record AppUserCreateDto(
        int CompanyId,
        int? BranchId,
        string FullName,
        string Email,
        string Password,
        byte Role
    );
}
