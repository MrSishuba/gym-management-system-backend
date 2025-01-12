using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Owner
    {
        [Key]
        public int Owner_ID { get; set; }

        public int User_ID { get; set; }

        [ForeignKey(nameof(User_ID))]
        public User User { get; set; }
    }
}
