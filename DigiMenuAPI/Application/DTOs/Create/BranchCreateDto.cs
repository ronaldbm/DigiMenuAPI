namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// DTO para crear una nueva sucursal.
    /// CompanyId se inyecta desde el JWT en el servicio — no viene en el body.
    /// El Slug es opcional: si no se envía, se genera automáticamente desde el Name.
    /// </summary>
    public record BranchCreateDto(
        string Name,
        string? Slug,
        string? Address,
        string? Phone,
        string? Email
    );
}