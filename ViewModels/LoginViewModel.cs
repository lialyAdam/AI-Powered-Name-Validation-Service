using System.ComponentModel.DataAnnotations;

namespace TakeCareOfUs.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage ="Email is reuired.")]
        [EmailAddress]
        public string Email  { get; set; }
        [Required(ErrorMessage = "Password is reuired.")]
        [DataType(DataType.Password)]

        public string  Password { get; set; }

        [Display(Name =" Remember me ?")]
        public bool RememberMe { get; set; }
    }
}
