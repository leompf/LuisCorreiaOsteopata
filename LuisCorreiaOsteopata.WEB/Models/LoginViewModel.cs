using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "*Campo Obrigatório")]
    [EmailAddress(ErrorMessage = "Email Inválido")]
    public string Username { get; set; } = null!;


    [Required(ErrorMessage = "*Campo Obrigatório")]
    public string Password { get; set; } = null!;


    public bool RememberMe { get; set; }

}
