namespace DigiMenuAPI.Application.DTOs.Update
{
    /// <summary>
    /// El rol y CompanyId no se pueden cambiar después de creado.
    /// Para reasignar a otra Branch se usa BranchId.
    /// </summary>
    public record AppUserUpdateDto(
        int Id,
        int? BranchId,
        string FullName,
        string Email,
        bool IsActive
    );
}
