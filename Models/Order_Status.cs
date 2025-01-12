using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Order_Status
    {
        [Key]
        public int Order_Status_ID { get; set; }

        public string Order_Status_Description { get; set; }
    }
}
