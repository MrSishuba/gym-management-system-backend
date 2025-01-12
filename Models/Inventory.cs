using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Inventory
    {
        [Key]
        public int Inventory_ID { get; set; }

        [Required]
        [StringLength(50)]
        public string Inventory_Item_Category { get; set;}

        [Required]
        [StringLength(50)]

        public string Inventory_Item_Name { get; set; }

        [Required]

        public int Inventory_Item_Quantity { get; set; }

        [Required]
        public string Inventory_Item_Photo { get; set; }

        public int? Supplier_ID { get; set; }

        [ForeignKey(nameof(Supplier_ID))]
        public Supplier Supplier { get; set; }

        public int? Received_Supplier_Order_ID { get; set; }

        [ForeignKey(nameof(Received_Supplier_Order_ID))]
        public Received_Supplier_Order Received_Supplier_Order { get; set; }

        public int Product_ID { get; set; }

        [ForeignKey(nameof(Product_ID))]
        public Product Product { get; set; }
    }
}
