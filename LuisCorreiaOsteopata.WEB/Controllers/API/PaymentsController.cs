using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stripe;
using Stripe.Checkout;

namespace LuisCorreiaOsteopata.WEB.Controllers.API;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;
    private readonly DataContext _context;

    public PaymentsController(IAppointmentRepository appointmentRepository,
        ILogger<PaymentsController> logger,
        IConfiguration configuration,
        DataContext context)
    {
        _logger = logger;
        _context = context;
    }


    [HttpPost("Webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        // Deserialize the event
        Event stripeEvent;
        try
        {
            stripeEvent = JsonConvert.DeserializeObject<Event>(json);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to deserialize Stripe event: {Message}", e.Message);
            return BadRequest();
        }

        _logger.LogInformation("Received Stripe event: {Type}", stripeEvent.Type);

        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return BadRequest();

            // Retrieve order ID from metadata
            if (!int.TryParse(session.Metadata["order_id"], out var orderId))
            {
                _logger.LogWarning("Invalid order ID in metadata for session {SessionId}", session.Id);
                return BadRequest();
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order not found for session {SessionId}", session.Id);
                return NotFound();
            }

            // Mark order as paid
            order.IsPaid = true;
            order.PaymentIntentId = session.PaymentIntentId;
            order.PaymentDate = DateTime.UtcNow;

            // Update the payment record if exists
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == session.PaymentIntentId);

            if (payment != null)
            {
                payment.Status = "Succeeded";
                payment.ConfirmedAt = DateTime.UtcNow;
            }
            else
            {
                _logger.LogWarning("Payment not found for session {SessionId}", session.Id);
            }
          

            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} processed successfully.", order.Id);
        }

        return Ok();
    }
}



