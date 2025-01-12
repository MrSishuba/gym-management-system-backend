using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Discount
    {
        [Key]
        public int Discount_ID { get; set; }

        public string Discount_Code { get; set; }

        [Required]
        public decimal Discount_Percentage { get; set; }

        [Required]
        public DateTime Discount_Date { get; set; }

        public DateTime End_Date { get; set; }
    }
}
