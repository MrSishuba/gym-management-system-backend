using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Payment_Type
    {
        [Key]
        public int Payment_Type_ID { get; set; }

        [Required, StringLength(255)]
        public string Payment_Type_Name { get; set; }

    }
}
