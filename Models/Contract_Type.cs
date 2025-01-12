using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Contract_Type
    {
        [Key]
        public int Contract_Type_ID { get; set; }

        [Required]
        [StringLength(100)]
        public string Contract_Type_Name { get; set; }

        [Required]
        public string Contract_Description { get; set; }
    }
}
