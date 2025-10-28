using LuisCorreiaOsteopata.WEB.Data.Entities;

namespace LuisCorreiaOsteopata.WEB.Data;

public interface IOrderRepository :IGenericRepository<Order>
{
    #region Cart
    Task<IQueryable<OrderDetailTemp>> GetCartAsync(string userName);
    Task<bool> AddProductToCartAsync(string username, int productId);
    Task DeleteCartItemAsync(int id);
    Task<(double itemTotal, double cartTotal, double newQuantity)?> UpdateCartQuantityAsync(int id, double quantity);
    Task<int> GetCartItemCountAsync(string username);
    #endregion

    #region Order Processing
    Task<bool> CreateOrderFromCartAsync(string username);
    Task<IQueryable<Order>> GetOrderAsync(string userName);
    #endregion

    #region CRUD Orders
    IQueryable<Order> GetAllOrders();
    Task<List<Order>> GetFilteredOrdersAsync(string? userId, string? orderNumber, DateTime? orderDate, DateTime? deliveryDate, DateTime? paymentDate, string? sortBy, bool sortDescending);
    Task<int> GetRemainingCreditsAsync(string userId);
    Task RefundCreditAsync(int orderDetailId);
    #endregion
}
