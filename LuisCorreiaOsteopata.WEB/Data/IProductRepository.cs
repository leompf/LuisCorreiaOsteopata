using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.WEB.Data;

public interface IProductRepository : IGenericRepository<Product>
{
    IQueryable<Product> GetAllWithUsers();

    Task<ICollection<Product>> GetAvailableProducts();

    IEnumerable<SelectListItem> GetComboProducts();

    IEnumerable<SelectListItem> GetComboProductCategory();

    Task<List<Product>> GetFilteredProductsAsync(string? nameFilter, string? categoryFilter, string? sortBy, bool sortDescending);
}
