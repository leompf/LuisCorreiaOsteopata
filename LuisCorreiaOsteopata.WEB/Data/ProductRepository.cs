using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.WEB.Data;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    private readonly DataContext _context;

    public ProductRepository(DataContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable GetAllWithUsers()
    {
        return _context.Products.Include(p => p.User);
    }

    public async Task<ICollection<Product>> GetAvailableProducts()
    {
        var products = await _context.Products
            .Where(p => p.IsAvailable == true)
            .ToListAsync();

        return products;
    }

    public IEnumerable<SelectListItem> GetComboProducts()
    {
        var list = _context.Products.Select(p => new SelectListItem
        {
            Text = p.Name,
            Value = p.Id.ToString(),
        }).ToList();

        list.Insert(0, new SelectListItem
        {
            Text = "(Select a product...)",
            Value = "0"
        });

        return list;
    }

    public IEnumerable<SelectListItem> GetComboProductCategory()
    {
        return Enum.GetValues(typeof(ProductType))
            .Cast<ProductType>()
            .Select(pt => new SelectListItem
            {
                Value = ((int)pt).ToString(),
                Text = pt.ToString()
            })
            .ToList();
    }
}


