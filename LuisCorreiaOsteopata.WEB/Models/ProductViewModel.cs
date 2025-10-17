using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Models;

public class ProductViewModel : Product
{
    [Display(Name = "Image")]
    public IFormFile? ImageFile { get; set; }

    public IEnumerable<SelectListItem>? ProductCategories { get; set; }
}
