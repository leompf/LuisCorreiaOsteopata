using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class Enable2faViewModel
{
    public string SharedKey { get; set; }

    public string QrCodeImage { get; set; }

    [Required, StringLength(6)]
    [Display(Name = "Código de verificação")]
    public string Code { get; set; }
}
