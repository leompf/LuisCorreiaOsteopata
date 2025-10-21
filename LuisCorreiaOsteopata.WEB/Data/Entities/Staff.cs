using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Data.Entities;

public class Staff : IEntity
{
    public int Id { get; set; }


    [Required]
    [Display(Name = "Names")]
    public string Names { get; set; }


    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }


    [Display(Name = "Full Name")]
    public string FullName => $"{Names} {LastName}";


    [Required]
    public string Email { get; set; }


    [StringLength(9)]
    public string Nif { get; set; }


    [Required]
    public string Phone { get; set; }


    [Required]
    public User User { get; set; }
}
