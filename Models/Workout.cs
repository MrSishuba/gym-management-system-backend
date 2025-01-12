using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Workout
    {
        [Key]
        public int Workout_ID { get; set; }

        [Required, StringLength(255)]
        public string Workout_Name { get; set; }

        [Required, StringLength(255)]
        public string Workout_Description { get; set; }


        [Required]
        public int Sets { get; set; }

        [Required]
        public int Reps { get; set; }

        public int Workout_Category_ID { get; set; }   

        [ForeignKey(nameof(Workout_Category_ID))]

        public Workout_Category Workout_Category { get; set; }


    }
}
