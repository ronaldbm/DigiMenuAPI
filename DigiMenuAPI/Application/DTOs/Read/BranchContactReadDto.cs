namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>Datos de contacto de una sucursal.</summary>
    public record BranchContactReadDto(
        int Id,
        string Name,
        string? Address,
        string? Phone,
        string? Email);
}
