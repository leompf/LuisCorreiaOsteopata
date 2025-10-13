using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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

    public IActionResult Index()
    {
        return View(_productRepository.GetAll().OrderBy(p => p.Name));
    }

    public async Task<IActionResult> ViewProducts()
    {
        var products = await _productRepository.GetAvailableProducts();

        return View(products);
    }

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
        return View();
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

        foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
        {
            Console.WriteLine(error.ErrorMessage);
        }

        return View(model);
    }
}
