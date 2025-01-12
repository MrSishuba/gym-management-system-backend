using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace av_motion_api.Models
{
    public class Employee
    {
        [Key]

        public int Employee_ID { get; set; }

        public DateTime Employment_Date { get; set; }

        [Required]
        public int Hours_Worked { get; set; }

        public int User_ID { get; set; }

        [ForeignKey(nameof(User_ID))]
        public User User { get; set; }


        public int Employee_Type_ID { get; set; }

        [ForeignKey(nameof(Employee_Type_ID))]

        public Employee_Type Employee_Type { get; set; }
    

        public int? Shift_ID { get; set; }

        [ForeignKey(nameof(Shift_ID))]
        public Shift Shift { get; set; }
    }
}
