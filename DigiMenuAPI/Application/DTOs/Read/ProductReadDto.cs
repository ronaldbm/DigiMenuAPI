namespace DigiMenuAPI.Application.DTOs.Read
{
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
    public record BranchProductReadDto(
        int Id,
        int BranchId,
        int ProductId,
        string ProductName,
        int CategoryId,
        string CategoryName,
        decimal Price,
        decimal? OfferPrice,
        string? ImageOverrideUrl,
        string? BaseImageUrl,
        int DisplayOrder,
        bool IsVisible
    );
}