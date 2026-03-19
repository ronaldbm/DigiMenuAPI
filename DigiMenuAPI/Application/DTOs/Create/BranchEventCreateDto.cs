using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>
    /// Datos para crear un nuevo evento de sucursal.
    /// Se recibe como multipart/form-data para permitir subir el flyer.
    /// </summary>
    public class BranchEventCreateDto
    {
        [Range(1, int.MaxValue)]
        public int BranchId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>Fecha del evento en formato ISO (YYYY-MM-DD).</summary>
        [Required]
        public DateOnly EventDate { get; set; }

        /// <summary>Hora de inicio. Null = todo el día.</summary>
        public TimeSpan? StartTime { get; set; }

        /// <summary>Hora de fin. Null = todo el día.</summary>
        public TimeSpan? EndTime { get; set; }

        public bool ShowPromotionalModal { get; set; }

        /// <summary>Imagen del flyer (opcional). Formatos: jpg, png, webp.</summary>
        public IFormFile? FlyerImage { get; set; }
    }
}
