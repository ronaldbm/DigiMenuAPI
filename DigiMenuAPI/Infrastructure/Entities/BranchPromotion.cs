using AppCore.Domain.Entities;
using AppCore.Infrastructure.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiMenuAPI.Infrastructure.Entities
{
    /// <summary>
    /// Slide promocional para el carrusel de bienvenida del menú público.
    /// Puede vincularse opcionalmente a un BranchProduct (2x1, Menú del día, etc.).
    /// </summary>
    public class BranchPromotion : BaseEntity
    {
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        /// <summary>Producto vinculado (opcional). FK Restrict — no se puede borrar el producto si tiene promos activas.</summary>
        public int? BranchProductId { get; set; }
        public BranchProduct? BranchProduct { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>Badge corto: "2x1", "Menú del día", "Especial". Máx 50 caracteres.</summary>
        [MaxLength(50)]
        public string? Label { get; set; }

        [MaxLength(500)]
        public string? PromoImageUrl { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? StartDate { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? EndDate { get; set; }

        /// <summary>Hora de inicio de la promoción (opcional). Si no se indica, aplica todo el día.</summary>
        [Column(TypeName = "time")]
        public TimeOnly? StartTime { get; set; }

        /// <summary>Hora de fin de la promoción (opcional).</summary>
        [Column(TypeName = "time")]
        public TimeOnly? EndTime { get; set; }

        /// <summary>Si true, aparece en el carrusel del menú público.</summary>
        public bool ShowInCarousel { get; set; } = true;

        /// <summary>Orden de aparición dentro de las promos del carrusel.</summary>
        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        // ── Metadatos de encuadre de imagen ──────────────────────────
        [MaxLength(20)] public string PromoObjectFit { get; set; } = "cover";
        [MaxLength(50)] public string PromoObjectPosition { get; set; } = "50% 50%";
    }
}
