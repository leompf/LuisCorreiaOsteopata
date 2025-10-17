using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class LoginWith2faViewModel
{
    [Required]
    [Display(Name = "Código de autenticação")]
    public string TwoFactorCode { get; set; }

    [Display(Name = "Lembrar este dispositivo")]
    public bool RememberDevice { get; set; }

    public bool RememberMe { get; set; }
}
