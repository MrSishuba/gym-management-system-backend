using av_motion_api.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.ViewModels
{
    public class RewardSetViewModel
    {
        public DateTime Reward_Issue_Date { get; set; }

        public int Reward_Type_ID { get; set; }

        public bool IsPosted { get; set; } = false;
    }
}
