using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Cart_Item
    {
        [Key]
        public int Cart_Item_ID { get; set; }

        public int Product_ID { get; set; }

        [ForeignKey(nameof(Product_ID))]
        public Product Product { get; set; }

        public int Quantity { get; set; }

        public int Cart_ID { get; set; }

        [ForeignKey(nameof(Cart_ID))]
        public Cart Cart { get; set; }
    }
}
