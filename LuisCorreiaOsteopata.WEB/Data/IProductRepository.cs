using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.WEB.Data;

public interface IProductRepository : IGenericRepository<Product>
{
    IQueryable GetAllWithUsers();

    Task<ICollection<Product>> GetAvailableProducts();

    IEnumerable<SelectListItem> GetComboProducts();

    IEnumerable<SelectListItem> GetComboProductCategory();
}
