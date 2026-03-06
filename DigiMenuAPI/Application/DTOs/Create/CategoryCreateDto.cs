namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// CompanyId NO se recibe del cliente — se inyecta desde el JWT en el servicio.
    /// Catálogo global de la empresa.
    /// </summary>
    public record CategoryCreateDto(
        string Name,
        int DisplayOrder,
        bool IsVisible
    );
}