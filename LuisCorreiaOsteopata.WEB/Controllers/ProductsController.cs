using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public async Task<IActionResult> ViewProducts()
    {
        var products = await _productRepository.GetAvailableProducts();

        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _productRepository.GetByIdAsync(id.Value);
        if (product == null)
        {
            return NotFound();
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
            return NotFound();
        }

        var product = await _productRepository.GetByIdAsync(id.Value);
        if (product == null)
        {
            return NotFound();
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
                    return NotFound();
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
            return NotFound();
        }

        var product = await _productRepository.GetByIdAsync(id.Value);
        if (product == null)
        {
            return NotFound();
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
}
