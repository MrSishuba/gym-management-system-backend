using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Booking_Time_Slot
    {
        [Key]
        public int Booking_Time_Slot_ID { get; set; }

        public int Booking_ID { get; set; }

        [ForeignKey(nameof(Booking_ID))]

        public Booking Booking { get; set; }

        public int Time_Slot_ID { get; set; }

        [ForeignKey(nameof(Time_Slot_ID))]

        public Time_Slot Time_Slot { get; set; }

    }
}
