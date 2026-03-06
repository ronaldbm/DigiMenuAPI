namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// CompanyId NO se recibe del cliente — se inyecta desde el JWT en el servicio.
    /// Etiqueta del catálogo global de la empresa.
    /// </summary>
    public record TagCreateDto(
        string Name,
        string? Color
    );
}