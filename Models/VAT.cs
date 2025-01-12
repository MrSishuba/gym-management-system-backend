using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class VAT
    {

        [Key]
        public int VAT_ID { get; set; }

        [Required]
        public decimal VAT_Percentage{ get; set; }

        [Required]
        public DateTime VAT_Date { get; set; }
    }
}
