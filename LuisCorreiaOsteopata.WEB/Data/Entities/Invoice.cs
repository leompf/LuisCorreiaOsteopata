namespace LuisCorreiaOsteopata.WEB.Data.Entities;

public class Invoice : IEntity
{
    public int Id { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public DateTime IssuedDate { get; set; }

    public decimal Total { get; set; }

}
