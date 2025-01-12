using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Member
    {
        [Key]
        public int Member_ID { get; set; }

        public int User_ID { get; set; }

        [ForeignKey(nameof(User_ID))]
        public User User { get; set; }


        public int Membership_Status_ID { get; set; }

        [ForeignKey(nameof(Membership_Status_ID))]
        public Membership_Status Membership_Status { get; set; }

    }
}

