using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Inspection_Type
    {
        [Key]
        public int Inspection_Type_ID { get; set; }

        [Required]
        [StringLength(50)]
        public string Inspection_Type_Name { get; set; }

        [Required]
        public string Inspection_Type_Criteria { get; set; }
    }
}
