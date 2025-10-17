namespace LuisCorreiaOsteopata.WEB.Data.Entities;

public class Payment : IEntity
{
    public int Id { get; set; }

    public string StripePaymentIntentId { get; set; } = null!;
    public string? StripeCustomerId { get; set; }

    public string PaymentMethod { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Status { get; set; } = "Pendente";

    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
   

    //FK
    public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;


    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
