using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuisCorreiaOsteopata.WEB.Models;

public class ProfileViewModel
{
    //Personal Details
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
    public DateOnly? BirthDate { get; set; }

    [StringLength(9)]
    public string? NIF { get; set; }

    public string? Role { get; set; }

    public string? Gender { get; set; }

    public byte? Height { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal? Weight { get; set; }

    public string? MedicalHistory { get; set; }

    public bool IsEditable { get; set; } = true;
}
