namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// CompanyId NO se recibe del cliente — se inyecta desde el JWT en el servicio.
    /// Producto del catálogo global de la empresa (sin precio, precio va en BranchProduct).
    /// </summary>
    public record ProductCreateDto(
        int CategoryId,
        string Name,
        string? ShortDescription,
        string? LongDescription,
        IFormFile? Image,
        List<int>? TagIds
    );
}