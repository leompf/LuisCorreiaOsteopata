using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class SignUpViewModel
{
    [Required(ErrorMessage = "*Campo Obrigatório")]
    [Display(Name = "First Name")]
    public string Names { get; set; } = null!;


    [Required(ErrorMessage = "*Campo Obrigatório")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = null!;


    [Required(ErrorMessage = "*Campo Obrigatório")]
    [EmailAddress(ErrorMessage = "Email Inválido")]
    public string Email { get; set; } = null!;


    [Required(ErrorMessage = "*Campo Obrigatório")]
    [Phone]
    [Display(Name = "Telefone")]
    public string PhoneNumber { get; set; } = null!;


    [Required(ErrorMessage = "*Campo Obrigatório")]
    [Display(Name = "Data de Nascimento")]
    public DateOnly Birthdate { get; set; }


    [Required(ErrorMessage = "*Campo Obrigatório")]
    [Display(Name = "Contribuinte")]
    [StringLength(9)]
    public string Nif { get; set; } = null!;


    [Required(ErrorMessage = "*Campo Obrigatório")]
    [MinLength(6)]
    public string Password { get; set; } = null!;


    [Required(ErrorMessage = "*Campo obrigatório")]
    [Compare("Password", ErrorMessage = "*A palavra-passe não coincide")]
    public string Confirm { get; set; } = null!;
}
