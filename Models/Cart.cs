using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Cart
    {
        [Key]
        public int Cart_ID { get; set; }

        public int Member_ID { get; set; }

        [ForeignKey(nameof(Member_ID))]
        public Member Member { get; set; }

        public ICollection<Cart_Item> Cart_Items { get; set; }
    }
}
