using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models
{
    public class SignUpViewModel
    {
        [Required(ErrorMessage = "*Campo obrigatório")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }


        [Required(ErrorMessage = "*Campo obrigatório")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }


        [Required(ErrorMessage = "*Campo obrigatório")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }


        [Required(ErrorMessage = "*Campo obrigatório")]
        [MinLength(6)]
        public string Password { get; set; }


        [Required(ErrorMessage = "*Campo obrigatório")]
        [Compare("Password", ErrorMessage = "*A palavra-passe não coincide")]
        public string Confirm { get; set; }
    }
}
