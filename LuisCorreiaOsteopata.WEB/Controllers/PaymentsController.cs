using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace LuisCorreiaOsteopata.WEB.Controllers;

public class PaymentsController : Controller
{
    private readonly DataContext _context;
    private readonly IConfiguration _configuration;
    private readonly IUserHelper _userHelper;

    public PaymentsController(DataContext context, 
        IConfiguration configuration,
        IUserHelper userHelper)
    {
        _context = context;
        _configuration = configuration;
        _userHelper = userHelper;
    }

    [HttpGet]
    public IActionResult Buy()
    {
        return View();
    }

    [HttpPost]
    public IActionResult CreateCheckoutSession(int credits)
    {
        var domain = $"{Request.Scheme}://{Request.Host}";
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = credits * 2000, 
                        Currency = "eur",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{credits} Prepaid Appointment Credits"
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            SuccessUrl = domain + "/Payments/Success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = domain + "/Payments/Cancel",
        };

        var service = new SessionService();
        Session session = service.Create(options);

        return Redirect(session.Url);
    }

    public async Task<IActionResult> Success(string session_id)
    {
        if (string.IsNullOrEmpty(session_id))
            return BadRequest("Invalid session ID");

        var sessionService = new SessionService();
        var session = await sessionService.GetAsync(session_id);

        // Fetch line items (products purchased)
        var lineItemService = new SessionLineItemService();
        var lineItems = await lineItemService.ListAsync(session.Id, new SessionLineItemListOptions
        {
            Limit = 100
        });

        foreach (var item in lineItems.Data)
        {
            // item.Description, item.Quantity, item.AmountTotal
            Console.WriteLine($"Product: {item.Description}, Quantity: {item.Quantity}, Total: {item.AmountTotal}");

            // TODO: Credit the user with pre-paid appointments based on item.Quantity
            // Example:
            // await _appointmentRepository.AddAppointmentCreditsAsync(userId, item.Quantity);
        }

        ViewBag.Message = "Pagamento realizado com sucesso!";
        return View();
    }

    public IActionResult Cancel()
    {
        return View();
    }
}
