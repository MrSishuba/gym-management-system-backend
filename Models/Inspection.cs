using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Inspection
    {
        [Key]
        public int Inspection_ID { get; set; }

        [Required] 
        public DateTime Inspection_Date{ get; set; }

        [Required]
        public string Inspection_Notes { get; set; }

        public int? Equipment_ID { get; set; }

        [ForeignKey(nameof(Equipment_ID))]
        public Equipment Equipment { get; set; }



        public int? Inventory_ID { get; set; }

        [ForeignKey(nameof(Inventory_ID))]
        public Inventory Inventory { get; set; }



        public int Inspection_Type_ID { get; set; }

        [ForeignKey(nameof(Inspection_Type_ID))]
        public Inspection_Type Inspection_Type { get; set; }

    

        public int Inspection_Status_ID { get; set; }

        [ForeignKey(nameof(Inspection_Status_ID))]

        public Inspection_Status Inspection_Status { get; set; }
    }
}
