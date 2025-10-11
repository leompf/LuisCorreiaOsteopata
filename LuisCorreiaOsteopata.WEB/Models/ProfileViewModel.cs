using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class ProfileViewModel
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
    public DateTime? BirthDate { get; set; }

    [StringLength(9)]
    public string? NIF { get; set; }

    public string? Role { get; set; }

    public string? CalendarId { get; set; }

    public string? CalendarName { get; set; }

    public bool IsEditable { get; set; } = true;
}
