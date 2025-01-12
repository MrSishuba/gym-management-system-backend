using System.ComponentModel.DataAnnotations;

namespace av_motion_api.ViewModels
{
    public class TerminateContractRequestViewModel
    {
        public int Contract_ID { get; set; }
        public int Member_ID { get; set; }

        public string? CustomReason { get; set; } // Optional: Custom reason for termination

        [Required]
        [EnumDataType(typeof(RequestedTerminationReasonType))]
        public RequestedTerminationReasonType Requested_Termination_Reason_Type { get; set; }


    }

    public enum RequestedTerminationReasonType
    {
        FeesTooExpensive,
        DifferentChallengeAtAnotherGym,
        UnhappyAtAVSFitness,
        Relocating,
        HealthIssues,
        PersonalOrFinancialCircumstances,
        LackOfTime,
        UnsatisfactoryCustomerService,
        Custom
    }
}

