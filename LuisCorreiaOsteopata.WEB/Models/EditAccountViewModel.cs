using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class EditAccountViewModel
{
    [Required]
    [Display(Name = "Nome")]
    public string Names { get; set; } = null!;

    [Required]
    [Display(Name = "Apelido")]
    public string LastName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [DataType(DataType.Date)]
    [Display(Name = "Data de Nascimento")]
    public DateOnly? Birthdate { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Password Atual")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Nova Password")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "As passwords não coincidem.")]
    [Display(Name = "Confirmar Nova Password")]
    public string? ConfirmPassword { get; set; }
}
