using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Update
{
    public class CustomerUpdateDto
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CreditLimit { get; set; }

        [Range(1, 100)]
        public int MaxOpenTabs { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MaxTabAmount { get; set; }

        public bool IsActive { get; set; }
    }
}
