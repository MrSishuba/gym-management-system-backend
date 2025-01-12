using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Equipment
    {
        [Key]
        public int Equipment_ID { get; set; }

        [Required]
        [StringLength(50)]
        public string Equipment_Name { get; set; }

        [Required]
        [StringLength(255)]
        public string Equipment_Description { get; set; }

       
        [StringLength(50)]
        public string? Size { get; set; }
    }
}
