namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// DTO para crear un usuario dentro de la empresa autenticada.
    /// CompanyId se inyecta desde el JWT — no viene en el body.
    ///
    /// Jerarquía de roles permitidos según el creador:
    ///   CompanyAdmin  → puede crear BranchAdmin (2) y Staff (3)
    ///   BranchAdmin   → puede crear Staff (3) solo en su propia Branch
    ///   Staff         → sin permiso de crear usuarios
    /// </summary>
    public record AppUserCreateDto(
        string FullName,
        string Email,
        byte Role,
        int? BranchId   // requerido para BranchAdmin y Staff, null para CompanyAdmin
    );
}