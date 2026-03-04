namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// Producto del catálogo global para el panel admin.
    /// Sin precio (el precio vive en BranchProduct).
    /// Incluye traducciones para gestión.
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
    /// Producto activado en una Branch para el menú público.
    /// El nombre y descripciones ya vienen resueltos al idioma solicitado.
    /// La imagen usa ImageOverrideUrl si existe, sino MainImageUrl del catálogo.
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
    /// Permite ver y editar la configuración de cada producto activado.
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