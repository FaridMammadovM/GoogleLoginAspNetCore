using Microsoft.AspNetCore.Identity;

namespace RegistrGmailFB.Models
{
    public class AppUser:IdentityUser
    {
        public string Fullname { get; set; }
    }
}
