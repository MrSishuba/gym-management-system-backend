using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Contract_History
    {
        [Key]
        public int Contract_History_ID { get; set; }
        public DateTime Subscription_Date { get; set; }
        public DateTime Expiry_Date { get; set; }
        public DateTime? Approval_Date { get; set; }
        public bool Terms_Of_Agreement { get; set; }
        public bool Approval_Status { get; set; }
        public string Approval_By { get; set; }
        public int Contract_Type_ID { get; set; }
        public int Payment_Type_ID { get; set; }
        public int Employee_ID { get; set; }
        public int? Owner_ID { get; set; }
        public string Filepath { get; set; }
        public bool IsTerminated { get; set; }
        public string Termination_Reason { get; set; }
        public DateTime Termination_Date { get; set; }

        public string Termination_Reason_Type { get; set; } // New column

        public int? Contract_ID { get; set; }

        [ForeignKey(nameof(Contract_ID))]
        public Contract Contract { get; set; }


        public int Member_ID { get; set; }

        [ForeignKey(nameof(Member_ID))]
        public Member Member { get; set; }
    }
}
