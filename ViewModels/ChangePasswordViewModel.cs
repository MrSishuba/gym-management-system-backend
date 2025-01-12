using System.ComponentModel.DataAnnotations;

namespace av_motion_api.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "The current password must be at least 6 characters long.")]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "The new password must be at least 6 characters long.")]
        public string NewPassword { get; set; }
    }
}
