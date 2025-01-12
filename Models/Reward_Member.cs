using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Reward_Member
    {
        [Key]
        public int Reward_Member_ID { get; set; }

        public bool IsRedeemed { get; set; } // New property to track redemption status

        public int Member_ID { get; set; }

        [ForeignKey(nameof(Member_ID))]

        public Member Member { get; set; }


        public int Reward_ID { get; set; }

        [ForeignKey(nameof(Reward_ID))]

        public Reward Reward { get; set; }
    }
}
