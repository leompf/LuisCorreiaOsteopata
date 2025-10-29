using LuisCorreiaOsteopata.WEB.Data.Entities;

namespace LuisCorreiaOsteopata.WEB.Models;

public class PagedProductsViewModel
{
    public IEnumerable<Product> Products { get; set; }
    public string SearchTerm { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalProducts { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalProducts / (double)PageSize);
}
