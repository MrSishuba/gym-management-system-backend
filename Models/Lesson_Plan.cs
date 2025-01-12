using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Lesson_Plan
    {
        [Key]
        public int Lesson_Plan_ID { get; set; }

        [Required, StringLength(255)]
        public string Program_Name { get; set; }

        [Required, StringLength(255)]
        public string Program_Description { get; set; }


    }
}
