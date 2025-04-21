using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.UpdateDTOs
{
    public class ItemPositionUpdate
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public int Position { get; set; }
    }
}
