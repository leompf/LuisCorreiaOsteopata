using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;

namespace LuisCorreiaOsteopata.WEB.Controllers;

public class PaymentsController : Controller
{
    private readonly DataContext _context;
    private readonly IUserHelper _userHelper;
    private readonly IEmailSender _emailSender;

    public PaymentsController(DataContext context,
        IConfiguration configuration,
        IUserHelper userHelper,
        IEmailSender emailSender,
        ILogger<PaymentsController> logger)
    {
        _context = context;
        _userHelper = userHelper;
        _emailSender = emailSender;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCheckoutSession(CheckoutViewModel model)
    {
        var user = await _userHelper.GetCurrentUserAsync();

        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.User)
            .Where(o => o.User.Id == user.Id && !o.IsPaid)
            .OrderByDescending(o => o.OrderDate)
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return RedirectToAction("Create", "Orders");
        }


        var lineItems = order.Items.Select(item => new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                UnitAmount = (long)(item.Price * 100),
                Currency = "eur",
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = item.Product.Name,
                },
            },
            Quantity = (long)item.Quantity
        }).ToList();

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { model.PaymentMethod },
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = $"{Request.Scheme}://{Request.Host}/Payments/Success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = Url.Action("Cancel", "Payments", null, Request.Scheme)
        };

        var service = new SessionService();
        Session session = service.Create(options);

        order.StripeSessionId = session.Id;
        order.PaymentIntentId = session.PaymentIntentId;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();

        return Redirect(session.Url);
    }

    public async Task<IActionResult> Success(string session_id)
    {
        var service = new SessionService();
        var session = await service.GetAsync(session_id);

        if (session.PaymentStatus == "paid")
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.StripeSessionId == session.Id);

            if (order != null)
            {
                //TODO: VERIFICAR ORDER NUMBER PORQUE A QUERY NAO FUNCIONA                

                order.IsPaid = true;
                order.PaymentDate = DateTime.UtcNow;
                order.OrderNumber = $"LC-{DateTime.Today.Year}-{order.Id:D4}";
                order.PaymentIntentId = session.PaymentIntentId;

                foreach (var item in order.Items)
                {
                    if (item.Product.ProductType == ProductType.Consulta)
                    {
                        item.RemainingUses = 1;
                    }
                    else if (item.Product.ProductType == ProductType.Pacote)
                    {
                        item.RemainingUses = 3;
                    }
                }

                _context.Update(order);
                await _context.SaveChangesAsync();

                var productList = string.Join(", ", order.Items.Select(i => i.Product.Name));

                var mail = $@"
                        <p>Olá {order.User.Names.Split(' ')[0]},</p>
                        <p>Recebemos o teu pedido e vamos processá-lo dentro de instantes. Os detalhes do pedido encontram-se em baixo:</p>
                        <p>Nº do Pedido: <b>{order.OrderNumber}</b></p
                        <p>Produto/Serviço: {productList}</p>
                        <p>Com os melhores cumprimentos,<br />
                        Luís Correia, Osteopata</p>";

                await _emailSender.SendEmailAsync(order.User.Email, $"Pedido Confirmado - ID {order.OrderNumber}", mail);
            }
            return View(order);
        }

        return View();
    }

    public IActionResult Cancel()
    {
        return View();
    }
}


//TODO: AUTENTICAÇÃO DE DOIS FATORES
//TODO: AUTENTICAÇÃO DE BIOMETRIA NA APP Móvel
//TODO: ALERTAS POR SMS E EMAIL
//TODO: DOWNLOAD DA FICHA DE CLIENTE PARA PDF E WORD
