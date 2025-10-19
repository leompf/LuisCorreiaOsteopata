using LuisCorreiaOsteopata.WEB.Data.Entities;

namespace LuisCorreiaOsteopata.WEB.Data;

public interface IOrderRepository :IGenericRepository<Order>
{
    #region

    #endregion
    Task<IQueryable<Order>> GetOrderAsync(string userName);


    Task<IQueryable<OrderDetailTemp>> GetCartAsync(string userName);


    //Task AddItemtoOrderAsync(AddItemViewModel model, string username);


    Task<(double itemTotal, double cartTotal, double newQuantity)?> UpdateCartQuantityAsync(int id, double quantity);


    Task DeleteCartItemAsync(int id);


    Task<bool> ConfirmOrderAsync(string username);

    Task<bool> AddProductToCartAsync(string username, int productId);

}
