using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace av_motion_api.Models
{
    public class User : IdentityUser<int>
    {
        [Key]
        public int User_ID { get; set; }

        [Required, StringLength(25)]
        public string Name { get; set; }

        [Required, StringLength(25)]
        public string Surname { get; set; }

        [Required, StringLength(13)]
        public string ID_Number { get; set; }

        //[Required, StringLength(50)]
        //public string Email_Address { get; set; }

        [Required, StringLength(255)]
        public string Physical_Address { get; set; }

        //[Required, StringLength(10)]
        //public string Contact_Number { get; set; }

        [Required]
        public string Photo { get; set; }

        //[Required, StringLength(25)]
        //public string Username { get; set; }

        //[Required, StringLength(15)]
        //public string Password { get; set; }

        [Required]
        public DateTime Date_of_Birth { get; set; }


        public int User_Status_ID { get; set; }

        [ForeignKey(nameof(User_Status_ID))]

        public User_Status User_Status { get; set; }

        public int User_Type_ID { get; set; }

        [ForeignKey(nameof(User_Type_ID))]

        public User_Type User_Type { get; set; }

        public DateTime? DeactivationDate { get; set; } // Add this property

        public DateTime? DeactivatedAt { get; set; }

        // Navigation properties for one-to-many relationships
        public ICollection<Member> Members { get; set; } = new List<Member>();
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<Owner> Owners { get; set; } = new List<Owner>();

    }
}
