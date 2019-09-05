using System.ComponentModel.DataAnnotations;

namespace WebVideoPortal.Models
{
    public class ResetPasswordModel
    {
        [Display(Name = "New Password")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Confirmed password does not match the new password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string ResetCode { get; set; }
    }
}