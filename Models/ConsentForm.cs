using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class ConsentForm
    {
        [Key]
        public int ConsentForm_ID { get; set; } // Primary key
        public string Member_Name { get; set; } // Name and Surname of the Member
        public string FileName { get; set; } // File name of the consent form

        public int Member_ID { get; set; }

        [ForeignKey(nameof(Member_ID))]
        public Member Member { get; set; }
    }
}
