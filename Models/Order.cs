using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Order
    {
        [Key]
        public int Order_ID { get; set; }

        public DateTime Order_Date { get; set; }

        public decimal Total_Price { get; set; }

        public bool IsCollected { get; set; }


        public int Member_ID { get; set; }

        [ForeignKey(nameof( Member_ID))]

        public Member Member { get; set; }


        public int Order_Status_ID { get; set; }

        [ForeignKey(nameof(Order_Status_ID))]

        public Order_Status Order_Status { get; set; }


        public ICollection<Order_Line> Order_Lines { get; set; }
    }
}
