namespace LuisCorreiaOsteopata.WEB.Data.Entities;

public class Payment : IEntity
{
    public int Id { get; set; }

    public string PaymentReference { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "EUR";

    public DateTime CreatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public string Status { get; set; } = "Pendente";

    //FK
    public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;


    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<AppointmentCredit> AppointmentCredits { get; set; } = new List<AppointmentCredit>();
}
