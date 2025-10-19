using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.WEB.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUserHelper _userHelper;
        private readonly ILogger<OrdersController> _logger;
        private readonly DataContext _context;

        public OrdersController(IOrderRepository orderRepository,
            IUserHelper userHelper,
            ILogger<OrdersController> logger,
            DataContext context)
        {
            _orderRepository = orderRepository;
            _userHelper = userHelper;
            _logger = logger;
            _context = context;
        }

        #region Cart
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = await _orderRepository.GetCartAsync(this.User.Identity.Name);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItemToCart(int id)
        {
            var success = await _orderRepository.AddProductToCartAsync(User.Identity.Name, id);

            if (!success)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Create));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCartItem(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            await _orderRepository.DeleteCartItemAsync(id.Value);
            return RedirectToAction("Create");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCartQuantity(int id, double quantityChange)
        {
            var result = await _orderRepository.UpdateCartQuantityAsync(id, quantityChange);

            if (result == null)
            {
                return NotFound();
            }

            var (itemTotal, cartTotal, newQuantity) = result.Value;

            return Json(new
            {
                itemTotal = itemTotal.ToString("C2"),
                cartTotal = cartTotal.ToString("C2"),
                newQuantity
            });
        }
        #endregion

        #region CRUD Orders
        //CREATE ORDER DETAIL
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder()
        {
            var response = await _orderRepository.ConfirmOrderAsync(this.User.Identity.Name);

            if (!response)
            {
                _logger.LogWarning("Failed to confirm order for user {Username}. Redirecting back to Cart.", this.User.Identity.Name);

                return RedirectToAction("Create");
            }

            return RedirectToAction("Checkout");
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            _logger.LogInformation("Accessing Checkout page for user {Username}.", this.User.Identity.Name);

            var orders = await _orderRepository.GetOrderAsync(this.User.Identity.Name);
            var lastOrder = await orders.FirstOrDefaultAsync();

            if (lastOrder == null)
            {
                _logger.LogWarning("No orders found for user {Username}. Redirecting to Create.", this.User.Identity.Name);
                return RedirectToAction("Create");
            }

            var model = new CheckoutViewModel
            {
                Order = lastOrder,
                BillingDetail = new BillingDetail()
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Accessed Orders Index page.");
            return View();
        }
        #endregion
    }
}

//TODO: Recuperar carrinho depois de uma Order ser criada se o utilizador nunca acabar a Order.
//TODO: Criar Faturação