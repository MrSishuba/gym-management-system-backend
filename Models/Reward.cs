using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Reward
    {
        [Key]
        public int Reward_ID { get; set; }

        [Required]
        public DateTime Reward_Issue_Date { get; set; }

        public bool IsPosted { get; set; } // New property to track posted status

        public int Reward_Type_ID { get; set; }

        [ForeignKey(nameof(Reward_Type_ID))]
        public Reward_Type Reward_Type { get; set; }

    }
}
