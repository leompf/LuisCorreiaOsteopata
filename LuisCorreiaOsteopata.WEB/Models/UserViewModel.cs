using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class UserViewModel
{
    public string Id { get; set; }

    [Display(Name = "Nome")]
    public string Name { get; set; }

    [Display(Name = "Data de Nascimento")]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    public DateTime? Birthdate { get; set; }

    public string Email { get; set; }

    public string NIF { get; set; }

    [Display(Name = "Perfil")]
    public string Role { get; set; }

    [Display(Name = "Contacto")]
    public string PhoneNumber { get; set; }
}
