using Microsoft.AspNetCore.Http;

namespace DigiMenuAPI.Application.DTOs.Update
{
    /// <summary>
    /// Datos para actualizar un evento de sucursal existente.
    /// Se recibe como multipart/form-data para permitir cambiar el flyer.
    /// </summary>
    public class BranchEventUpdateDto
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateOnly EventDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool ShowPromotionalModal { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Nueva imagen del flyer. Si null, se conserva la existente.</summary>
        public IFormFile? FlyerImage { get; set; }

        public string? FlyerObjectFit { get; set; }
        public string? FlyerObjectPosition { get; set; }
    }
}
