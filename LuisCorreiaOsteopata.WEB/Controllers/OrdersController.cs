using AspNetCoreGeneratedDocument;
using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LuisCorreiaOsteopata.WEB.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUserHelper _userHelper;
        private readonly ILogger<OrdersController> _logger;
        private readonly IBillingDetailRepository _billingDetailRepository;
        private readonly DataContext _context;

        public OrdersController(IOrderRepository orderRepository,
            IUserHelper userHelper,
            ILogger<OrdersController> logger,
            IBillingDetailRepository billingDetailRepository,
            DataContext context)
        {
            _orderRepository = orderRepository;
            _userHelper = userHelper;
            _logger = logger;
            _billingDetailRepository = billingDetailRepository;
            _context = context;
        }

        #region Cart
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = await _orderRepository.GetCartAsync(User.Identity.Name);
            if (model == null)
            {
                _logger.LogWarning("No cart found for user {Username}.", User.Identity.Name);
                return RedirectToAction("Index", "Home");
            }

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

            var cartCount = await _orderRepository.GetCartItemCountAsync(User.Identity.Name);

            return Json(new
            {
                success = true,
                cartCount
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var cartCount = await _orderRepository.GetCartItemCountAsync(User.Identity.Name);
            return Json(new { cartCount });
        }
        #endregion

        #region Order Processing
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder()
        {
            var success = await _orderRepository.CreateOrderFromCartAsync(User.Identity.Name);

            if (!success)
            {
                _logger.LogWarning("Failed to create order for user {Username}. Redirecting back to Cart.", User.Identity.Name);
                return RedirectToAction("Create");
            }

            return RedirectToAction("Checkout");
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            _logger.LogInformation("Accessing Checkout page for user {Username}.", User.Identity.Name);

            var orders = await _orderRepository.GetOrderAsync(User.Identity.Name);
            var lastOrder = await orders.FirstOrDefaultAsync();

            if (lastOrder == null)
            {
                _logger.LogWarning("No orders found for user {Username}. Redirecting to Cart.", User.Identity.Name);
                return RedirectToAction("Create");
            }

            var billingDetails = await _billingDetailRepository.GetBillingDetailsByUserAsync(User.Identity.Name);

            var model = new CheckoutViewModel
            {
                Order = lastOrder,
                BillingDetail = new BillingDetail(),
                BillingDetails = billingDetails
            };

            return View(model);
        }
        #endregion

        #region Orders
        [HttpGet]
        [Authorize(Roles = "Colaborador,Administrador")]
        public async Task<IActionResult> ViewAllOrders(string? userId, string? orderNumber, DateTime? orderDate, DateTime? deliveryDate, DateTime? paymentDate, string? sortBy, bool sortDescending = false)
        {
            _logger.LogInformation("Accessed Orders Index page.");

            var orders = await _orderRepository.GetFilteredOrdersAsync(userId, orderNumber, orderDate, deliveryDate, paymentDate, sortBy, sortDescending);

            var model = new OrderListViewModel
            {
                OrderUserFilter = userId,
                OrderNumberFilter = orderNumber,
                OrderDateFilter = orderDate,
                OrderDeliveryDateFilter = deliveryDate,
                OrderPaymentDateFilter = orderDate,
                Orders = orders
            };

            ViewBag.DefaultSortBy = sortBy;
            ViewBag.DefaultSortDescending = sortDescending;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_OrdersTable", model.Orders);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userHelper.GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.BillingDetail)
            .Where(o => o.User.Id == user.Id)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var order = _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.BillingDetail)
                .Include(o => o.User)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
        #endregion
    }
}
