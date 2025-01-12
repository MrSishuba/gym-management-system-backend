using System.ComponentModel.DataAnnotations;

namespace av_motion_api.ViewModels
{
    public class TerminateContractViewModel
    {
        public int Contract_ID { get; set; }
        public int Member_ID { get; set; }
        public string Reason { get; set; } // Optional: reason for termination

        [Required]
        [EnumDataType(typeof(TerminationReasonType))]
        public TerminationReasonType Termination_Reason_Type { get; set; }
    }


    public enum TerminationReasonType
    {
        Banned,
        Upgraded,
        SpecialCase,
        ContractExpired
    }
}
