using System.ComponentModel.DataAnnotations;

namespace WebAdvert.Web.Models.Accounts
{
    public class ConfirmAccount
    {
        [Required]
        [Display(Name="Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Code is Required")]
        public string ConfirmationCode { get; set; }
    }
}
