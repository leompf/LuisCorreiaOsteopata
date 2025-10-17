using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace LuisCorreiaOsteopata.WEB.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUserHelper _userHelper;
        private readonly DataContext _context;

        public OrdersController(IOrderRepository orderRepository,
            IUserHelper userHelper,
            DataContext context)
        {
            _orderRepository = orderRepository;
            _userHelper = userHelper;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Create()
        {
            var model = await _orderRepository.GetDetailTempsAsync(this.User.Identity.Name);
            return View(model);
        }

        public async Task<IActionResult> AddProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var user = await _userHelper.GetCurrentUserAsync();
            if (user == null)
            {
                return Challenge();
            }

            var cartItem = await _context.OrderDetailsTemp
                .Include(o => o.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.User.Id == user.Id && o.Product.Id == id);

            if (cartItem == null)
            {
                cartItem = new OrderDetailTemp
                {
                    Product = product,
                    Price = product.Price,
                    Quantity = 1,
                    User = user
                };
                _context.OrderDetailsTemp.Add(cartItem);
            }
            else
            {
                cartItem.Quantity++;
                _context.OrderDetailsTemp.Update(cartItem);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Create));
        }

        public async Task<IActionResult> DeleteItem(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            await _orderRepository.DeleteDetailTempAsync(id.Value);
            return RedirectToAction("Create");
        }

        [HttpPost]
        public async Task<IActionResult> ModifyQuantity(int id, double quantityChange)
        {
            var result = await _orderRepository.ModifyOrderDetailTempQuantityAsync(id, quantityChange);

            if (result == null)
                return NotFound();

            var (itemTotal, cartTotal, newQuantity) = result.Value;

            return Json(new
            {
                itemTotal = itemTotal.ToString("C2"),
                cartTotal = cartTotal.ToString("C2"),
                newQuantity
            });
        }

        public async Task<IActionResult> ConfirmOrder()
        {
            var response = await _orderRepository.ConfirmOrderAsync(this.User.Identity.Name);

            if (response)
            {
                return RedirectToAction("Checkout");
            }

            return RedirectToAction("Create");
        }

        public async Task<IActionResult> Checkout()
        {
            var orders = await _orderRepository.GetOrderAsync(this.User.Identity.Name);
            var lastOrder = await orders.FirstOrDefaultAsync();

            if (lastOrder == null)
                return RedirectToAction("Create");

            var model = new CheckoutViewModel
            {
                Order = lastOrder,
                BillingDetail = new BillingDetail()
            };

            return View(model);
        }
    }
}

//TODO: Recuperar carrinho depois de uma Order ser criada se o utilizador nunca acabar a Order.
//TODO: Criar Faturação