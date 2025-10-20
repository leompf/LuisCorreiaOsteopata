using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.WEB.Models;

public class ProductListViewModel
{
    public string? NameFilter {  get; set; }

    public string? CategoryFilter {  get; set; }

    public IEnumerable<Product> Products { get; set; }

    public IEnumerable<SelectListItem> Categories {  get; set; }
}
