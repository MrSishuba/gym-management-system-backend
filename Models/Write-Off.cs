using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Write_Off
    {
        [Key]
        public int Write_Off_ID { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Write_Off_Reason { get; set; }


        public int Inventory_ID { get; set; }

        [ForeignKey(nameof(Inventory_ID))]

        public Inventory Inventory { get; set; }

        [Required]
        public int Write_Off_Quantity { get; set; } 
    }
}
