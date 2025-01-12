using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class User_Type
    {
        [Key]
        public int User_Type_ID { get; set; }

        public string User_Type_Name { get; set; }
    }
}
