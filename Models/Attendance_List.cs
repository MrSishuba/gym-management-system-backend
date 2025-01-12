using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Attendance_List
    {
        [Key]
        public int Attendance_ID { get; set; }

        [Required]

        public int Number_Of_Bookings { get; set; }

        [Required]
        public int Members_Present { get; set; }

        [Required]

        public int Members_Absent { get; set; }

        public int Time_Slot_ID { get; set; }

        [ForeignKey(nameof(Time_Slot_ID))]
        public Time_Slot Time_Slot { get; set; }
    }
}
