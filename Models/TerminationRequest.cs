using av_motion_api.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class TerminationRequest
    {
        [Key]
        public int TerminationRequest_ID { get; set; }
        public int Contract_ID { get; set; }

        [ForeignKey(nameof(Contract_ID))]
        public Contract Contract { get; set; }
        public int Member_ID { get; set; }

        [ForeignKey(nameof(Member_ID))]
        public Member Member { get; set; }
        public string CustomReason { get; set; }
        public RequestedTerminationReasonType Requested_Termination_Reason_Type { get; set; }


    }
}
