using System.ComponentModel.DataAnnotations;

namespace AppCore.Domain.Entities
{
    /// <summary>
    /// Configuración de SEO y analytics del menú público de una Branch.
    /// Incluye metadatos para motores de búsqueda y píxeles de seguimiento.
    ///
    /// Separada del resto para poder actualizarla sin afectar
    /// el tema visual ni la identidad del negocio.
    ///
    /// Todos los campos son opcionales — el menú funciona sin SEO configurado.
    ///
    /// Relación 1:1 con Branch.
    /// </summary>
    public class BranchSeo : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        // ── SEO ───────────────────────────────────────────────────────
        /// <summary>Título para la pestaña del navegador y resultados de búsqueda.</summary>
        [MaxLength(100)]
        public string? MetaTitle { get; set; }

        /// <summary>Descripción para resultados de búsqueda. Recomendado: 150-160 caracteres.</summary>
        [MaxLength(300)]
        public string? MetaDescription { get; set; }

        // ── Analytics ─────────────────────────────────────────────────
        /// <summary>ID de Google Analytics 4. Ejemplo: "G-XXXXXXXXXX".</summary>
        [MaxLength(50)]
        public string? GoogleAnalyticsId { get; set; }

        /// <summary>ID del píxel de Meta (Facebook). Ejemplo: "123456789012345".</summary>
        [MaxLength(50)]
        public string? FacebookPixelId { get; set; }
    }
}
