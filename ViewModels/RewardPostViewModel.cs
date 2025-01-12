namespace av_motion_api.ViewModels
{
    public class RewardPostViewModel
    {
        public int RewardId { get; set; }
        public bool TriggerCheck { get; set; } = true; // New property to trigger the check
    }

    public class QualifyingMembersVM
    {
        public int Member_ID { get; set; }
    }
}
