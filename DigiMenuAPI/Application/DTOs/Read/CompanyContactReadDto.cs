namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>Datos de contacto de la empresa para el dashboard del admin.</summary>
    public record CompanyContactReadDto(
        int Id,
        string Name,
        string? Email,
        string? Phone,
        string? CountryCode);
}
