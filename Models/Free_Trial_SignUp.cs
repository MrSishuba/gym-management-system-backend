using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Free_Trial_SignUp
    {
        [Key]
        public int Guest_ID { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string ID_Number { get; set; }
        public DateTime DateActivated { get; set; }
        public DateTime DateExpired { get; set; }
        public string FreeTrialCode { get; set; }
    }
}
