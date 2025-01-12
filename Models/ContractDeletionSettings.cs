namespace av_motion_api.Models
{
    public class ContractDeletionSettings
    {
        public int DeletionTimeValue { get; set; } = 3; // Default to 3
        public string DeletionTimeUnit { get; set; } = "years"; // Default to years
    }
}
