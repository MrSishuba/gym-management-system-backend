using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;

namespace av_motion_api.Models
{
    public class Booking
    {
        [Key]
        public int Booking_ID { get; set; }

        public int Member_ID { get; set; }

        [ForeignKey(nameof(Member_ID))]

        public Member Member { get; set; }

  

      /*  public int Attendance_List_ID { get; set; }

        [ForeignKey(nameof(Attendance_List_ID))]

        public Attendance_List Attendance_List { get; set; }*/

    }
}
