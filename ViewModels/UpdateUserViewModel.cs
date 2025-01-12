namespace av_motion_api.ViewModels
{
    public class UpdateUserViewModel
    {
        public string Name { get; set; }

        public string Surname { get; set; }

        public string Email { get; set; }

        public string Physical_Address { get; set; }

        public string PhoneNumber { get; set; }

        public IFormFile Photo { get; set; }
    }
}
