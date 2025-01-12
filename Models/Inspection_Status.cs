using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Inspection_Status
    {
        [Key]
        public int Inspection_Status_ID { get; set; }

        [Required]
        [StringLength(50)]
        public string Inspection_Status_Description { get; set; }
    }
}
