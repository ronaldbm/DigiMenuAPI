namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// Producto del catálogo global — versión compacta para modales de selección.
    /// Permite al admin elegir qué productos activar en una Branch (BranchProduct).
    /// </summary>
    public class ProductSummaryDto
    {
        public int Id { get; init; }
        public int CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string? MainImageUrl { get; init; }
    }

    /// <summary>
    /// Producto del catálogo global para listados del panel admin.
    /// Sin precio (el precio vive en BranchProduct por sucursal).
    /// El texto (nombre, descripciones) vive en las Translations.
    /// </summary>
    public class ProductReadDto
    {
        public int Id { get; init; }
        public int CompanyId { get; init; }
        public int CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string? MainImageUrl { get; init; }
        public List<TagReadDto> Tags { get; init; } = [];
        public List<ProductTranslationReadDto> Translations { get; init; } = [];
        public DateTime CreatedAt { get; init; }
    }

    /// <summary>
    /// Producto para listados del panel admin.
    /// El nombre ya viene resuelto al idioma solicitado (con fallback).
    /// TagCount evita cargar las traducciones de cada tag en la lista.
    /// </summary>
    public class ProductListItemDto
    {
        public int Id { get; init; }
        public int CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string? MainImageUrl { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ShortDescription { get; init; }
        public int TagCount { get; init; }
    }

    /// <summary>
    /// Producto completo para el formulario de edición del panel admin.
    /// Incluye traducciones completas y tags con sus propias traducciones.
    /// El texto (nombre, descripciones) vive en las Translations.
    /// </summary>
    public class ProductAdminReadDto
    {
        public int Id { get; init; }
        public int CompanyId { get; init; }
        public int CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string? MainImageUrl { get; init; }
        public List<TagReadDto> Tags { get; init; } = [];          // tags con sus traducciones incluidas
        public List<ProductTranslationReadDto> Translations { get; init; } = [];
        public DateTime CreatedAt { get; init; }
        public DateTime? ModifiedAt { get; init; }
    }

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
        string? LongDescription,
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