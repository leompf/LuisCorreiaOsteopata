using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuisCorreiaOsteopata.WEB.Data.Entities;

public class Patient : IEntity
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Names")]
    public string Names {  get; set; }

    [Required]
    [Display(Name = "Last Name")]
    public string LastName {  get; set; }

    [Display(Name = "Full Name")]
    public string FullName => $"{Names} {LastName}";

    public DateOnly? BirthDate {  get; set; }

    [Required]
    public string Email { get; set; }

    [StringLength(9)]
    public string? Nif {  get; set; }

    public string? Phone {  get; set; }

    public string? Gender { get; set; }

    public byte? Height { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal? Weight { get; set; }

    public string? MedicalHistory { get; set; }

    [Required]
    public User User { get; set; }
}
