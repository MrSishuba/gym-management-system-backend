using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Product_Type
    {
        [Key]
        public int Product_Type_ID { get; set; }

        [Required]
        [StringLength(100)]
        public string Type_Name { get; set; }
    }
}
