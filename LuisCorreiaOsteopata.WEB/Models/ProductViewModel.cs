using LuisCorreiaOsteopata.WEB.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class ProductViewModel : Product
{
    [Display(Name = "Image")]
    public IFormFile? ImageFile { get; set; }
}
