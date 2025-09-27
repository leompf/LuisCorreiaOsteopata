using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.Library.Data.Entities;

public class Staff : IEntity
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }


    [Required]
    [Display(Name = "Last Name")]
    public string LastName {  get; set; }


    [Required]
    public User User { get; set; }
}
