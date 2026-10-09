using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaTrust.Models
{
    public class DeliveryRegion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryFee { get; set; } = 7.00m;

        [MaxLength(100)]
        public string EstimatedDeliveryTime { get; set; } = "24h - 48h";

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;
    }
}
