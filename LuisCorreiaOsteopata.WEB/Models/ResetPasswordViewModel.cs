using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Token { get; set; } = null!;

    [Required(ErrorMessage = "*Campo Obrigatório")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "*Campo Obrigatório")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "A palavra-passe não coincide.")]
    public string ConfirmPassword { get; set; } = null!;
}
