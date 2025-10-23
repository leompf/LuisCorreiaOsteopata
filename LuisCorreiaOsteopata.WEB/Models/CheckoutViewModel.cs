using LuisCorreiaOsteopata.WEB.Data.Entities;

namespace LuisCorreiaOsteopata.WEB.Models;

public class CheckoutViewModel
{
    public Order? Order { get; set; } = null!;

    public BillingDetail? BillingDetail { get; set; }

    public string PaymentMethod { get; set; }


    public List<BillingDetail>? BillingDetails { get; set; }
}
