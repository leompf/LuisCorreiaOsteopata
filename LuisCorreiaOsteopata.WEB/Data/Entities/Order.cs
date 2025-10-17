using System.ComponentModel.DataAnnotations;

namespace LuisCorreiaOsteopata.WEB.Data.Entities;

public class Order : IEntity
{
    public int Id { get; set; }


    [Display(Name = "Order Number")]
    public string OrderNumber { get; set; } = string.Empty;


    [Required]
    [Display(Name = "Order Date")]
    [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd hh:mm tt}", ApplyFormatInEditMode = false)]
    public DateTime OrderDate { get; set; }


    [Required]
    [Display(Name = "Delivery Date")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy hh:mm tt}", ApplyFormatInEditMode = false)]
    public DateTime DeliveryDate { get; set; }

    [Display(Name = "Payment Date")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy hh:mm tt}", ApplyFormatInEditMode = false)]
    public DateTime? PaymentDate { get; set; }


    [Display(Name = "Dados de Faturação")]
    public BillingDetail? BillingDetail { get; set; }


    [Required]
    public User User { get; set; }


    public bool IsPaid { get; set; } = false;


    public string? StripeSessionId { get; set; }


    public string? PaymentIntentId { get; set; }




    public IEnumerable<OrderDetail> Items { get; set; }


    [DisplayFormat(DataFormatString = "{0:N0}")]
    public int Lines => Items == null ? 0 : Items.Count();


    [DisplayFormat(DataFormatString = "{0:N2}")]
    public double Quantity => Items == null ? 0 : Items.Sum(i => i.Quantity);


    [DisplayFormat(DataFormatString = "{0:C2}")]
    public decimal Value => Items == null ? 0 : Items.Sum(i => i.Value);


    [Display(Name = "Order date")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy hh:mm tt}", ApplyFormatInEditMode = false)]
    public DateTime? OrderDateLocal => this.OrderDate == null ? null : this.OrderDate.ToLocalTime();
}
