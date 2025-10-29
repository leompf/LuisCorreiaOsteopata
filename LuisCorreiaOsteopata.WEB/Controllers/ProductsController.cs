using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace LuisCorreiaOsteopata.WEB.Controllers;

public class ProductsController : Controller
{
    private readonly IProductRepository _productRepository;
    private readonly IUserHelper _userHelper;
    private readonly IConverterHelper _converterHelper;
    private readonly IImageHelper _imageHelper;

    public ProductsController(IProductRepository productRepository,
        IUserHelper userHelper,
        IConverterHelper converterHelper,
        IImageHelper imageHelper)
    {
        _productRepository = productRepository;
        _userHelper = userHelper;
        _converterHelper = converterHelper;
        _imageHelper = imageHelper;
    }

    [Authorize(Roles = "Administrador,Colaborador")]
    [HttpGet]
    public async Task<IActionResult> Index(string? name, string? category, string? sortBy, bool sortDescending = true)
    {
        var products = await _productRepository.GetFilteredProductsAsync(name, category, sortBy, sortDescending);

        var model = new ProductListViewModel
        {
            NameFilter = name,
            CategoryFilter = category,
            Products = products,
            Categories = _productRepository.GetComboProductCategory()
        };

        ViewBag.DefaultSortColumn = sortBy ?? "Name";
        ViewBag.DefaultSortDescending = sortDescending;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_ProductsTable", model.Products);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ViewProducts(string searchTerm,decimal? minPrice, decimal? maxPrice, int page = 1, int pageSize = 12)
    {
        var productsQuery = await _productRepository.GetAvailableProducts();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            productsQuery = productsQuery.Where(p => p.Name.Contains(searchTerm)).ToList();

        if (minPrice.HasValue)
            productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value).ToList();

        if (maxPrice.HasValue)
            productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value).ToList();

        var totalProducts =  productsQuery.Count();
        var products =   productsQuery
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

        var model = new PagedProductsViewModel
        {
            Products = products,
            PageNumber = page,
            PageSize = pageSize,
            TotalProducts = totalProducts,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            SearchTerm = searchTerm
        };


        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return new NotFoundViewResult("ProductNotFound");
        }

        var product = await _productRepository.GetByIdAsync(id.Value);
        if (product == null)
        {
            return new NotFoundViewResult("ProductNotFound");
        }

        return View(product);
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Create()
    {
        var model = new ProductViewModel
        {
            ProductCategories = _productRepository.GetComboProductCategory()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        if (ModelState.IsValid)
        {
            var path = string.Empty;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                path = await _imageHelper.UploadImageAsync(model.ImageFile, "products");
            }

            var product = _converterHelper.ToProduct(model, path, true);

            product.User = await _userHelper.GetUserByEmailAsync(this.User.Identity.Name);
            await _productRepository.CreateAsync(product);
            return RedirectToAction(nameof(Index));
        }

        model.ProductCategories = _productRepository.GetComboProductCategory();
        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return new NotFoundViewResult("ProductNotFound");
        }

        var product = await _productRepository.GetByIdAsync(id.Value);
        if (product == null)
        {
            return new NotFoundViewResult("ProductNotFound");
        }

        var model = _converterHelper.ToProductViewModel(product);
        model.ProductCategories = _productRepository.GetComboProductCategory();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductViewModel model)
    {

        if (ModelState.IsValid)
        {
            try
            {
                var path = model.ImageUrl;

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    path = await _imageHelper.UploadImageAsync(model.ImageFile, "products");
                }

                var product = _converterHelper.ToProduct(model, path, false);


                product.User = await _userHelper.GetUserByEmailAsync(this.User.Identity.Name);
                await _productRepository.UpdateAsync(product);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _productRepository.ExistAsync(model.Id))
                {
                    return new NotFoundViewResult("ProductNotFound");
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return new NotFoundViewResult("ProductNotFound");
        }

        var product = await _productRepository.GetByIdAsync(id.Value);
        if (product == null)
        {
            return new NotFoundViewResult("ProductNotFound");
        }

        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        try
        {
            await _productRepository.DeleteAsync(product);
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException != null && ex.InnerException.Message.Contains("DELETE"))
            {
                ViewBag.ErrorTitle = $"{product.Name} provalvemente está a ser usado!";
                ViewBag.ErrorMessage = $"{product.Name} não pode ser apagado visto haverem encomendas que o usam.</br></br>" +
                    $"Experimente primeiro apagar todas as encomendas que o estão a usar," +
                    $"e torne novamente a apagá-lo";
            }

            return View("Error");
        }

    }

    public IActionResult ProductNotFound()
    {
        return View();
    }
}
