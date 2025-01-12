using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Shift
    {
        [Key]
        public int Shift_ID { get; set; }

        [Required]
        public int Shift_Number { get; set; }

        [Required]
        public TimeSpan Start_Time { get; set; }

        [Required]
        public TimeSpan End_Time { get; set;}


    }
}
