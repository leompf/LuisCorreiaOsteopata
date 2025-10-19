using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.WEB.Data;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    private readonly DataContext _context;
    private readonly ILogger<OrderRepository> _logger;
    private readonly IUserHelper _userHelper;

    public OrderRepository(DataContext context,
        ILogger<OrderRepository> logger,
        IUserHelper userHelper) : base(context)
    {
        _context = context;
        _logger = logger;
        _userHelper = userHelper;
    }

    #region CRUD Cart
    public async Task<IQueryable<OrderDetailTemp>> GetCartAsync(string userName)
    {
        try
        {
            _logger.LogInformation("Fetching temporary order details(Cart) for user {Username}.", userName);

            var user = await _userHelper.GetUserByEmailAsync(userName);
            if (user == null)
            {
                _logger.LogWarning("User {Username} not found.", userName);
                return null;
            }

            return _context.OrderDetailsTemp
                .Include(p => p.Product)
                .Where(o => o.User == user)
                .OrderByDescending(o => o.Product.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch temporary order details for user {Username}.", userName);
            return null;
        }
    }

    public async Task<bool> AddProductToCartAsync(string username, int productId)
    {
        _logger.LogInformation("Adding Cart item with ID {Id}.", productId);

        try
        {
            var user = await _userHelper.GetUserByEmailAsync(username);
            if (user == null) return false;

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            var cartItem = await _context.OrderDetailsTemp
                .Include(o => o.Product)
                .FirstOrDefaultAsync(o => o.User.Id == user.Id && o.Product.Id == productId);

            if (cartItem == null)
            {
                cartItem = new OrderDetailTemp
                {
                    Product = product,
                    Price = product.Price,
                    Quantity = 1,
                    User = user
                };
                _context.OrderDetailsTemp.Add(cartItem);
            }
            else
            {
                cartItem.Quantity++;
                _context.OrderDetailsTemp.Update(cartItem);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Product {ProductId} added to cart for {Username}.", productId, username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add product {ProductId} to cart for user {Username}.", productId, username);
            return false;
        }
    }

    public async Task DeleteCartItemAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deleting Cart item with ID {Id}.", id);

            var orderDetailTemp = await _context.OrderDetailsTemp.FindAsync(id);
            if (orderDetailTemp == null)
            {
                _logger.LogWarning("Item with ID {Id} not found in Cart.", id);
                return;
            }

            _context.OrderDetailsTemp.Remove(orderDetailTemp);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Item with ID {Id} deleted successfully from Cart.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Cart Item with ID {Id}.", id);
        }
    }

    public async Task<(double itemTotal, double cartTotal, double newQuantity)?> UpdateCartQuantityAsync(int id, double quantity)
    {
        try
        {
            _logger.LogInformation("Updating quantity for Cart Item ID {Id} by {Quantity}.", id, quantity);

            var item = await _context.OrderDetailsTemp
               .Include(o => o.Product)
               .FirstOrDefaultAsync(o => o.Id == id);

            if (item == null)
            {
                _logger.LogWarning("Cart item with ID {Id} not found.", id);
                return null;
            }

            item.Quantity += quantity;
            if (item.Quantity < 1)
                item.Quantity = 1;

            _context.OrderDetailsTemp.Update(item);
            await _context.SaveChangesAsync();

            var cartTotal = await _context.OrderDetailsTemp
                .SumAsync(i => (double)i.Price * i.Quantity);

            var itemTotal = (double)item.Price * item.Quantity;

            _logger.LogInformation(
                       "Cart item ID {Id} updated. ItemTotal: {ItemTotal}, CartTotal: {CartTotal}, NewQuantity: {Quantity}.",
                       id, itemTotal, cartTotal, item.Quantity);

            return (itemTotal, cartTotal, item.Quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to modify quantity for for Cart item ID {Id}.", id);
            return null;
        }
    }
    #endregion


    public async Task<IQueryable<Order>> GetOrderAsync(string userName)
    {
        try
        {
            _logger.LogInformation("Fetching orders for user {Username}.", userName);

            var user = await _userHelper.GetUserByEmailAsync(userName);
            if (user == null)
            {
                _logger.LogWarning("User {Username} not found.", userName);
                return null;
            }

            if (await _userHelper.IsUserInRoleAsync(user, "Admin"))
            {
                _logger.LogInformation("User {Username} is Admin, fetching all orders.", userName);
                return _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Items)
                    .ThenInclude(p => p.Product)
                    .OrderByDescending(o => o.OrderDate);
            }

            _logger.LogInformation("Fetching orders for user {Username} only.", userName);
            return _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.User == user)
                .OrderByDescending(o => o.OrderDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch orders for user {Username}.", userName);
            return null;
        }
    }

    public async Task<bool> ConfirmOrderAsync(string username)
    {
        try
        {
            var user = await _userHelper.GetUserByEmailAsync(username);
            if (user == null)
            {
                _logger.LogWarning("User {Username} not found.", username);
                return false;
            }

            var orderTmps = await _context.OrderDetailsTemp
                .Include(o => o.Product)
                .Where(o => o.User == user)
                .ToListAsync();

            if (orderTmps == null || orderTmps.Count == 0)
            {
                _logger.LogWarning("No temporary order details found for user {Username}.", username);
                return false;
            }

            var details = orderTmps.Select(o => new OrderDetail
            {
                Price = o.Price,
                Product = o.Product,
                Quantity = o.Quantity,
            }).ToList();

            var order = new Order
            {
                OrderDate = DateTime.UtcNow,
                Items = details,
                User = user,
            };

            await CreateAsync(order);
            _context.OrderDetailsTemp.RemoveRange(orderTmps);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order successfully saved for {Username} with {ItemCount} items.", username, details.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming order for {Username}.", username);
            return false;
        }
    }
}
