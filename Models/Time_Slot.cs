using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Time_Slot
    {
        [Key]
        public int Time_Slot_ID { get; set; }

        public DateTime Slot_Date { get; set; }

        [Required]
        public DateTime Slot_Time { get; set; }

        [Required]
        public bool Availability { get; set; }

        public int Lesson_Plan_ID { get; set; }

        [ForeignKey(nameof(Lesson_Plan_ID))]

        public Lesson_Plan Lesson_Plan { get; set; }


        public int Employee_ID { get; set; }

        [ForeignKey(nameof(Employee_ID))]

        public Employee Employee { get; set; }




    }
}
