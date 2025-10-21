using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Data.Entities;

public class User : IdentityUser
{
    [Required]
    [Display(Name = "Names")]
    public string Names { get; set; }


    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = false)]
    public DateOnly? Birthdate { get; set; }

    
    [StringLength(9, ErrorMessage = "O NIF deve conter 9 dígitos")]
    public string? Nif { get; set; }


    [Display(Name = "Image")]
    public Guid ImageId { get; set; }

    public string? CalendarId { get; set; }

    public string? StripeCustomerId { get; set; }

    public ICollection<Payment>? Payments { get; set; } 
}
