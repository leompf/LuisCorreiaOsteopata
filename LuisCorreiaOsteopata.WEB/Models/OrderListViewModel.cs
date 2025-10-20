using LuisCorreiaOsteopata.WEB.Data.Entities;

namespace LuisCorreiaOsteopata.WEB.Models
{
    public class OrderListViewModel
    {
        public string? OrderUserFilter { get; set; }

        public string? OrderNumberFilter { get; set; }

        public DateTime? OrderDateFilter { get; set; }
        
        public DateTime? OrderDeliveryDateFilter {  get; set; }

        public DateTime? OrderPaymentDateFilter { get; set; }

        public IEnumerable<Order>? Orders { get; set; }
    }
}
