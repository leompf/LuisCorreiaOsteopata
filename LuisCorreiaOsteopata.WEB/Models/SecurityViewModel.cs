using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class SecurityViewModel
{
    [Display(Name = "Autenticação de Dois Fatores Ativa")]
    public bool Is2faEnabled { get; set; }


    [Display(Name = "Chave Partilhada")]
    public string? SharedKey { get; set; }


    [Display(Name = "QR Code")]
    public string? QrCodeImage { get; set; }
}
