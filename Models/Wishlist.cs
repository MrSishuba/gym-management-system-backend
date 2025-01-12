using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Wishlist
    {
        [Key]
        public int Wishlist_ID { get; set; }

        public int Member_ID { get; set; }

        [ForeignKey(nameof(Member_ID))]
        public Member Member { get; set; }

        public ICollection<Wishlist_Item> Wishlist_Items { get; set; }
    }
}
