using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.Library.Data.Entities;

public class Patient : IEntity
{
    public int Id { get; set; }


    [Required]
    [Display(Name = "First Name")]
    public string FirstName {  get; set; }


    [Required]
    [Display(Name = "Last Name")]
    public string LastName {  get; set; }


    [Required]
    public string Email { get; set; }


    [StringLength(9)]
    public string? Nif {  get; set; }


    public string? Phone {  get; set; }


    [Required]
    public User User { get; set; }
}
