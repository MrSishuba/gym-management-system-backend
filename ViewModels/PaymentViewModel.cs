using av_motion_api.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.ViewModels
{
    public class PaymentViewModel
    {
        public decimal Amount { get; set; }
        public DateTime Payment_Date { get; set; }
        public int Order_ID { get; set; }
        public int Contract_ID { get; set; }
        public int Payment_Type_ID { get; set; }
    }
}
