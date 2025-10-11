using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class UserListViewModel
{
    public string Id { get; set; } 
    public string Name { get; set; }

    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    public DateTime? Birthdate { get; set; }
    public string Email { get; set; }
    public string NIF { get; set; }
    public string Role { get; set; }
    public string PhoneNumber { get; set; }
}
