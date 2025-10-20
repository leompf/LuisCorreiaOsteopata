using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace LuisCorreiaOsteopata.WEB.Data;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    private readonly DataContext _context;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(DataContext context,
        ILogger<ProductRepository> logger) : base(context)
    {
        _context = context;
        _logger = logger;
    }


    #region CRUD Products
    public IQueryable<Product> GetAllWithUsers()
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

    public async Task<List<Product>> GetFilteredProductsAsync(string? nameFilter, string? categoryFilter, string? sortBy, bool sortDescending)
    {
        var products = GetAllWithUsers();

        if (!string.IsNullOrEmpty(nameFilter))
        {
            products = products.Where(p => EF.Functions.Like(p.Name, $"%{nameFilter}%"));
        }

        if (!string.IsNullOrEmpty(categoryFilter) &&
            Enum.TryParse<ProductType>(categoryFilter, true, out var parsedCategory))
        {
            products = products.Where(p => p.ProductType == parsedCategory);
        }

        products = sortBy switch
        {
            "Name" => sortDescending
                ? products.OrderByDescending(p => p.Name)
                : products.OrderBy(p => p.Name),

            "Price" => sortDescending
                ? products.OrderByDescending(p => p.Price)
                : products.OrderBy(p => p.Price),

            "Category" => sortDescending
                ? products.OrderByDescending(p => p.ProductType)
                : products.OrderBy(p => p.ProductType),

            "LastPurchase" => sortDescending
                ? products.OrderByDescending(p => p.LastPurchase)
                : products.OrderBy(p => p.LastPurchase),

            "LastSale" => sortDescending
                ? products.OrderByDescending(p => p.LastSale)
                : products.OrderBy(p => p.LastSale),

            "IsAvailable" => sortDescending
                ? products.OrderByDescending(p => p.IsAvailable)
                : products.OrderBy(p => p.IsAvailable),

            "Stock" => sortDescending
                ? products.OrderByDescending(p => p.Stock)
                : products.OrderBy(p => p.Stock),

            _ => products.OrderBy(p => p.Name)
        };


        return await products.ToListAsync();
    }
    #endregion

    #region Helper Methods
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
    #endregion

    
}


