using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Membership_Status
    {
        [Key]
        public int Membership_Status_ID { get; set; }

        [Required]
        public string Membership_Status_Description { get; set; }
    }
}
