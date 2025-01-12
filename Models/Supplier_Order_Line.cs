using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Supplier_Order_Line
    {
        [Key]
        public int Supplier_Order_Line_ID { get; set; }

        public int Supplier_Quantity { get; set; }


        public int Product_ID { get; set; }

        [ForeignKey(nameof(Product_ID))]

        public Product Product { get; set; }

        public decimal? Purchase_Price { get; set; } 

        public decimal? Unit_Price { get; set; } 

        public int Supplier_Order_ID { get; set; }

        [ForeignKey(nameof(Supplier_Order_ID))]
        public Supplier_Order Supplier_Order { get; set; } 
    }
}