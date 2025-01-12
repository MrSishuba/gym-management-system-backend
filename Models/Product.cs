using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Product
    {
        [Key]
        public int Product_ID { get; set; }

        [Required, StringLength(100)]
        public string Product_Name { get; set; }

        [Required, StringLength(100)]
        public string Product_Description { get; set; }

        [Required]
        public string Product_Img { get; set; }

        public decimal? Purchase_Price { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal Unit_Price { get; set; }

        [Required]
        public string Size { get; set; }

        public int Product_Category_ID { get; set; }

        [ForeignKey(nameof(Product_Category_ID))]
        public Product_Category Product_Category { get; set; }

        public int Product_Type_ID { get; set; }

        [ForeignKey(nameof(Product_Type_ID))]
        public Product_Type Product_Type { get; set; }

    }
}
