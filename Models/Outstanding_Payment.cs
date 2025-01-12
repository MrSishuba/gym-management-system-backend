using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Outstanding_Payment
    {
        [Key]
        public int Outstanding_Payment_ID { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Due_Date { get; set; }

        [Required]
        public decimal Amount_Due { get; set; } = 0;

        public int Member_ID { get; set; }

        [ForeignKey(nameof(Member_ID))]

        public Member Member { get; set; }

        public int Payment_ID { get; set; }

        [ForeignKey(nameof(Payment_ID))]

        public Payment Payment { get; set; }
    }
}
