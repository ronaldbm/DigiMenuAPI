namespace DigiMenuAPI.Application.DTOs.Update
{
    /// <summary>
    /// Edición de datos básicos de un usuario.
    ///
    /// Restricciones:
    ///   - CompanyId no se puede cambiar (siempre del tenant autenticado)
    ///   - Role no se puede cambiar después de creado — eliminar y recrear si es necesario
    ///   - IsActive se maneja por separado con PATCH /toggle-active
    ///   - BranchId se puede reasignar dentro de la misma empresa
    /// </summary>
    public record AppUserUpdateDto(
        int Id,
        string FullName,
        string Email,
        int? BranchId 
    );
}