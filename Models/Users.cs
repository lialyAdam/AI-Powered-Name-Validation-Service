using Microsoft.AspNetCore.Identity;

namespace TakeCareOfUs.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;


    }
}
