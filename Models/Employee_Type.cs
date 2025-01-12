using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Employee_Type
    {
        [Key]
        public int Employee_Type_ID { get; set; }

        [Required]
        [StringLength(50)]
        public string Job_Title { get; set; }

        [Required]
        [StringLength(255)]
        public string Job_Description { get; set; }
    }
}
