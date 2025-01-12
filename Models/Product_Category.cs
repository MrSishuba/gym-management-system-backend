using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Product_Category
    {
        [Key]
        public int Product_Category_ID { get; set; }

        [Required, StringLength(225)]
        public string Category_Name { get; set; }

        // Foreign Key to Product_Type
        public int Product_Type_ID { get; set; }

        // Navigation property
        public Product_Type Product_Type { get; set; }
    }
}
