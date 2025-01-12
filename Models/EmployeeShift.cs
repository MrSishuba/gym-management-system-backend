using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class EmployeeShift
    {
        [Key]
        public int EmployeeShift_ID { get; set; }

        public int Employee_ID { get; set; }

        [ForeignKey(nameof(Employee_ID))]
        public Employee Employee { get; set; }

        public int Shift_ID { get; set; }

        [ForeignKey(nameof(Shift_ID))]
        public Shift Shift { get; set; }

        public DateTime Shift_Start_Time { get; set; }
        public DateTime? Shift_End_Time { get; set; } // Nullable
    }
}
