using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Data.Entities;

public class Product : IEntity
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50, ErrorMessage = "The field {0} can contain {1} characters length.")]
    [Display(Name = "Nome")]
    public string Name { get; set; }


    public string? Description { get; set; }


    [Required]
    [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
    [Display(Name = "Preço")]
    public decimal Price { get; set; }


    [Required]
    [Display(Name = "Categoria")]
    public ProductType ProductType { get; set; }


    [Display(Name = "Image")]
    public string? ImageUrl { get; set; }


    [Display(Name = "Última Compra")]
    public DateTime? LastPurchase { get; set; }


    [Display(Name = "Última Venda")]
    public DateTime? LastSale { get; set; }


    [Display(Name = "Disponível")]
    public bool IsAvailable { get; set; }


    [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = false)]
    public double Stock { get; set; }


    public User? User { get; set; }


    public string? ImageFullPath
    {
        get
        {
            if (string.IsNullOrEmpty(ImageUrl))
            {
                return null;
            }

            return $"https://localhost:7298{ImageUrl.Substring(1)}";
        }
    }
}
