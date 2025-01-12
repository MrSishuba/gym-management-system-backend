using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Reward_Type
    {
        [Key]
        public int Reward_Type_ID { get; set; }

        [Required]
        public string Reward_Type_Name { get; set; }

        [Required]
        public string Reward_Criteria { get; set; }
    }
}
