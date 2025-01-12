using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.ViewModels
{
    public class UserViewModel
    {

        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Physical_Address { get; set; }
        public string PhoneNumber { get; set; }
        public IFormFile? Photo { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }
        public string ID_Number { get; set; }
        public DateTime Date_of_Birth { get; set; }

        [Range(1, 2, ErrorMessage = "User Status ID must be between 1 and 2.")]
        public int User_Status_ID { get; set; }

        [Range(1, 3, ErrorMessage = "User Type ID must be between 1 and 3.")]
        public int User_Type_ID { get; set; }

    }
}
