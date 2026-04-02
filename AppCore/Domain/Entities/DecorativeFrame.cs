using System.ComponentModel.DataAnnotations;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Marco decorativo predefinido para el menú público.
    /// El SVG usa currentColor para heredar el color secundario de la marca.
    /// Gestionado por SuperAdmin. Inmutable en producción (IDs 1-8 son fijos).
    /// </summary>
    public class DecorativeFrame : BaseEntity
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;

        /// <summary>Categoría temática: "fine-dining", "cafe", "bar", "universal".</summary>
        [Required, MaxLength(30)]
        public string Category { get; set; } = null!;

        /// <summary>Contenido SVG completo (2-5KB). Usa currentColor para heredar brand color.</summary>
        [Required]
        public string SvgContent { get; set; } = null!;

        public int DisplayOrder { get; set; }
    }
}
