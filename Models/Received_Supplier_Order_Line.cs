using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Received_Supplier_Order_Line
    {
        [Key]
        public int Received_Supplier_Order_Line_ID { get; set; }

        public int Received_Supplier_Order_ID { get; set; }

        [ForeignKey(nameof(Received_Supplier_Order_ID))]

        public Received_Supplier_Order Received_Supplier_Order { get; set; }


        public int Supplier_Order_Line_ID { get; set; }

        [ForeignKey(nameof(Supplier_Order_Line_ID))]

        public Supplier_Order_Line Supplier_Order_Line { get; set; }
      

        public int Product_ID { get; set; }

        [ForeignKey(nameof(Product_ID))]

        public Product Product { get; set; }

        public int Received_Supplies_Quantity { get; set; }
    }
}