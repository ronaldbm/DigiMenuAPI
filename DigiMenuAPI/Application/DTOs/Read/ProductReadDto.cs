namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// Producto del catálogo global — versión compacta para modales de selección.
    /// Permite al admin elegir qué productos activar en una Branch (BranchProduct).
    /// </summary>
    public record ProductSummaryDto(
        int Id,
        int CategoryId,
        string CategoryName,
        string Name,
        string? MainImageUrl
    );

    /// <summary>
    /// Producto del catálogo global para listados del panel admin.
    /// Sin precio (el precio vive en BranchProduct por sucursal).
    /// </summary>
    public record ProductReadDto(
        int Id,
        int CompanyId,
        int CategoryId,
        string CategoryName,
        string Name,
        string? ShortDescription,
        string? LongDescription,
        string? MainImageUrl,
        List<TagReadDto> Tags,
        List<ProductTranslationReadDto> Translations,
        DateTime CreatedAt
    );

    /// <summary>
    /// Producto completo para el formulario de edición del panel admin.
    /// Incluye traducciones completas y tags con sus propias traducciones.
    /// </summary>
    public record ProductAdminReadDto(
        int Id,
        int CompanyId,
        int CategoryId,
        string CategoryName,
        string Name,
        string? ShortDescription,
        string? LongDescription,
        string? MainImageUrl,
        List<TagReadDto> Tags,           // tags con sus traducciones incluidas
        List<ProductTranslationReadDto> Translations,
        DateTime CreatedAt,
        DateTime? ModifiedAt
    );

    /// <summary>
    /// Producto activado en una Branch para el menú público.
    /// Nombre y descripciones ya vienen resueltos al idioma solicitado.
    /// La imagen usa ImageOverrideUrl si existe, sino MainImageUrl del catálogo global.
    /// </summary>
    public record BranchProductMenuDto(
        int Id,
        int ProductId,
        string Name,
        string? ShortDescription,
        string? ImageUrl,
        decimal Price,
        decimal? OfferPrice,
        int DisplayOrder,
        List<TagMenuDto> Tags
    );

    /// <summary>
    /// BranchProduct para el panel admin de la Branch.
    /// Permite ver y editar la configuración de cada producto activado en esa sucursal.
    /// </summary>
    public class BranchProductReadDto
    {
        public int Id { get; init; }
        public int BranchId { get; init; }
        public int ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public decimal? OfferPrice { get; init; }
        public string? ImageOverrideUrl { get; init; }
        public string? BaseImageUrl { get; init; }
        public int DisplayOrder { get; init; }
        public bool IsVisible { get; init; }
    }
}