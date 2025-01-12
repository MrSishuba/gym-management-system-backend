using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Workout_Category
    {
        [Key]
        public int Workout_Category_ID { get; set; }

        [Required]
        [StringLength(255)]
        public string Workout_Category_Name { get; set; }

        [Required]
        public string Workout_Category_Description { get; set; }

    }
}
