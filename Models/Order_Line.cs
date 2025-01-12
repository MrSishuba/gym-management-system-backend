using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Order_Line
    {
        [Key]
        public int Order_Line_ID { get; set; }

        public int Product_ID { get; set; }

        [ForeignKey(nameof(Product_ID))]
        public Product Product { get; set; }


        public int Order_ID { get; set; }

        [ForeignKey(nameof(Order_ID))]
        public Order Order { get; set; }

        public int Quantity { get; set; }

        public decimal Unit_Price { get; set; }
    }
}
