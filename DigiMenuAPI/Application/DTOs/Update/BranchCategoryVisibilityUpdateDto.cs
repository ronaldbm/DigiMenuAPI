namespace DigiMenuAPI.Application.DTOs.Update
{
    /// <summary>
    /// Cambia la visibilidad de todos los BranchProducts de una categoría en una sucursal.
    /// Permite mostrar u ocultar un bloque completo de productos por categoría.
    /// </summary>
    public record BranchCategoryVisibilityUpdateDto(bool IsVisible);
}
