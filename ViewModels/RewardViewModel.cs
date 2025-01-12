namespace av_motion_api.ViewModels
{
    public class RewardViewModel
    {
        public int Reward_ID { get; set; }
        public DateTime Reward_Issue_Date { get; set; }
        public string Reward_Type_Name { get; set; }
        public bool IsPosted { get; set; }
    }
}
