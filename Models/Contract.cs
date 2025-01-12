using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;

namespace av_motion_api.Models
{
    public class Contract
    {

        [Key]
        public int Contract_ID { get; set; }

        [Required]
        public DateTime Subscription_Date { get; set; }

        [Required]
        public DateTime Expiry_Date { get; set; }


        [Required]
        public DateTime? Approval_Date { get; set; }

        [Required]
        public bool Terms_Of_Agreement { get; set; }

        [Required]
        public bool Approval_Status { get; set; }

        [Required]
        public string Approval_By { get; set; }

        [Required]
        public string Filepath { get; set; } //the uploaded sigend contracy


        [Required]
        public bool IsTerminated { get; set; } = false; // New property for soft delete

        public int Contract_Type_ID { get; set; }

        [ForeignKey(nameof(Contract_Type_ID))]
        public Contract_Type Contract_Type { get; set; }

        public int Payment_Type_ID { get; set; }

        [ForeignKey(nameof(Payment_Type_ID))]
        public Payment_Type Payment_Type { get; set; }

        public int Member_ID { get; set; }

        [ForeignKey(nameof(Member_ID))]
        public Member Member { get; set; }

        public int Employee_ID { get; set; }

        [ForeignKey(nameof(Employee_ID))]
        public Employee Employee { get; set; }

        public int? Owner_ID { get; set; }

        [ForeignKey(nameof(Owner_ID))]
        public Owner Owner { get; set; }
    }

}
