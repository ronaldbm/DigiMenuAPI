using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    public class CustomerCreateDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CreditLimit { get; set; } = 0;

        [Range(1, 100)]
        public int MaxOpenTabs { get; set; } = 1;

        [Range(0, double.MaxValue)]
        public decimal MaxTabAmount { get; set; } = 0;
    }
}
