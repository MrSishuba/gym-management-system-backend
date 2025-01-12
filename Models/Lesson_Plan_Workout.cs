using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Lesson_Plan_Workout
    {
        [Key]
        public int Lesson_Plan_Workout_ID { get; set; }

        public int Workout_ID { get; set; }

        [ForeignKey(nameof(Workout_ID))]

        public Workout Workout { get; set; }

        public int Lesson_Plan_ID { get; set; }

        [ForeignKey(nameof(Lesson_Plan_ID))]

        public Lesson_Plan Lesson_Plan { get; set; }


    }
}
