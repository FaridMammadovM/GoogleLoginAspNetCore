using System.ComponentModel.DataAnnotations;

namespace RegistrGmailFB.ViewModel
{
    public class RegistrVM
    {
        [Required]
        public string Fullname { get; set; }
        [Required]
        public string UserName { get; set; }

        [Required, DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password")]
        public string RepeatPassword { get; set; }
    }
}
