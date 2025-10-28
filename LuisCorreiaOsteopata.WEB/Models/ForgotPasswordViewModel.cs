using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}
