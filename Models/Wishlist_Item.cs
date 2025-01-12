using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Wishlist_Item
    {
        [Key]
        public int Wishlist_Item_ID { get; set; }

        public string? Size { get; set; }

        public int Product_ID { get; set; }

        [ForeignKey(nameof(Product_ID))]
        public Product Product { get; set; }

        public int Wishlist_ID { get; set; }

        [ForeignKey(nameof(Wishlist_ID))]
        public Wishlist Wishlist { get; set; }
    }
}
