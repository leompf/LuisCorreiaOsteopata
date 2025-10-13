using LuisCorreiaOsteopata.WEB.Data.Entities;

namespace LuisCorreiaOsteopata.WEB.Data;

public interface IOrderRepository :IGenericRepository<Order>
{
    Task<IQueryable<Order>> GetOrderAsync(string userName);


    Task<IQueryable<OrderDetailTemp>> GetDetailTempsAsync(string userName);


    //Task AddItemtoOrderAsync(AddItemViewModel model, string username);


    Task ModifyOrderDetailTempQuantityAsync(int id, double quantity);


    Task DeleteDetailTempAsync(int id);


    Task<bool> ConfirmOrderAsync(string username);
}
