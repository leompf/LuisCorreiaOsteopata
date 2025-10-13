using Google.Apis.Calendar.v3.Data;
using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Helpers;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace LuisCorreiaOsteopata.WEB.Controllers.API;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUserHelper _userHelper;
    private readonly ILogger<PaymentsController> _logger;
    private readonly IConfiguration _configuration;

    public PaymentsController(IAppointmentRepository appointmentRepository,
        IUserHelper userHelper,
        ILogger<PaymentsController> logger,
        IConfiguration configuration)
    {
        _appointmentRepository = appointmentRepository;
        _userHelper = userHelper;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("webhook")]
    public async Task <IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        // Get the Stripe webhook secret from your configuration
        const string endpointSecret = "whsec_7ade6c049580c8f0a9281c5a4132c9415a569974c410d553c51c0c449e59022f";

        try
        {
            var stripeSignature = Request.Headers["Stripe-Signature"];

            // Validate signature and construct event
            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, endpointSecret);

            // Handle Checkout Session Completed
            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null && session.Metadata != null)
                {
                    // Read userId and package quantity from metadata
                    var userId = session.Metadata["UserId"];
                    var packageQuantity = int.Parse(session.Metadata["Quantity"]);

                    // Credit the user with pre-paid appointments
                    await _appointmentRepository.AddAppointmentCreditsAsync(userId, packageQuantity);
                }
            }

            // You can handle other events if needed
            return Ok();
        }
        catch (StripeException ex)
        {
            // Stripe validation failed
            Console.WriteLine($"Stripe error: {ex.Message}");
            return BadRequest();
        }
        catch
        {
            return StatusCode(500);
        }
    }
}



