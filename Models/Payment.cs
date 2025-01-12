using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Payment
    {
        [Key]
        public int Payment_ID { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Payment_Date { get; set; }

        public int? Order_ID { get; set; }

        [ForeignKey(nameof(Order_ID))]

        public Order Order { get; set; }


        public int Contract_ID { get; set; }

        [ForeignKey(nameof(Contract_ID))]

        public Contract Contract { get; set; }

        public int Payment_Type_ID { get; set; }

        [ForeignKey(nameof(Payment_Type_ID))]

        public Payment_Type Payment_Type { get; set; }
    }
}
