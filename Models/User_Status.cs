using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class User_Status
    {
        [Key]
        public int User_Status_ID { get; set; }

        public string User_Status_Description { get; set; }
    }
}
