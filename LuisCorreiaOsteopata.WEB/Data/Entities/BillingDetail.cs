using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Data.Entities;

public class BillingDetail : IEntity
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    [Display(Name = "Nome")]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    [Display(Name = "Nome da Empresa")]
    public string? CompanyName { get; set; }

    [Required, MaxLength(150)]
    [Display(Name = "Morada")]
    public string Address { get; set; } = null!;

    [Required, StringLength(8)]
    [Display(Name = "Código Postal")]
    [RegularExpression(@"^\d{4}-\d{3}$", ErrorMessage = "O código postal deve estar no formato xxxx-xxx")]
    public string ZipCode { get; set; } = null!;

    [Required, MaxLength(25)]
    [Display(Name = "Localidade")]
    public string City { get; set; } = null!;

    [Required, MaxLength(20)]
    [Display(Name = "Distrito")]
    public string State { get; set; } = null!;

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required, Phone]
    public string Phone { get; set; } = null!;

    [Required, StringLength(9)]
    public string NIF { get; set; } = null!;

    public User User { get; set; }
}
