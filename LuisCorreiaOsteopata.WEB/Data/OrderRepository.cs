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

    public async Task<int> GetCartItemCountAsync(string username)
    {
        try
        {
            var user = await _userHelper.GetUserByEmailAsync(username);
            if (user == null)
            {
                _logger.LogWarning("User {Username} not found when getting cart count.", username);
                return 0;
            }

            var count = (int)await _context.OrderDetailsTemp
                .Where(o => o.User.Id == user.Id)
                .SumAsync(o => o.Quantity);

            _logger.LogInformation("User {Username} has {Count} items in cart.", username, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cart count for user {Username}.", username);
            return 0;
        }
    }
    #endregion

    #region Order Processing
    public async Task<bool> CreateOrderFromCartAsync(string username)
    {
        try
        {
            var user = await _userHelper.GetUserByEmailAsync(username);
            if (user == null) return false;

            var cartItems = await _context.OrderDetailsTemp
                .Include(o => o.Product)
                .Where(o => o.User == user)
                .ToListAsync();

            var existingOrder = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.User == user && !o.IsPaid)
                .FirstOrDefaultAsync();

            if (!cartItems.Any())
            {
                if (existingOrder != null)
                {
                    _logger.LogInformation("Cart is empty — deleting existing unpaid order {OrderId} for user {UserId}.", existingOrder.Id, user.Id);
                    _context.Orders.Remove(existingOrder);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("No cart items found for User {userId}.", user.Id);
                return false;
            }            

            if (existingOrder != null)
            {
                _logger.LogInformation("Recovered existing unpaid order {OrderId} for user {userId}.", existingOrder.Id, user.Id);

                _context.OrderDetails.RemoveRange(existingOrder.Items);

                existingOrder.Items = cartItems.Select(o => new OrderDetail
                {
                    Price = o.Price,
                    Product = o.Product,
                    Quantity = o.Quantity
                }).ToList();

                existingOrder.OrderTotal = existingOrder.Items.Sum(d => d.Price * (decimal)d.Quantity);
                existingOrder.OrderDate = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }

            var details = cartItems.Select(o => new OrderDetail
            {
                Price = o.Price,
                Product = o.Product,
                Quantity = o.Quantity,
            }).ToList();

            var total = details.Sum(d => d.Price * (decimal)d.Quantity);
            var nextValue = _context.Database
                .SqlQueryRaw<int>("SELECT NEXT VALUE FOR dbo.OrderNumberSequence")
                .AsEnumerable()
                .First();

            var order = new Order
            {
                OrderDate = DateTime.Now,                
                User = user,
                Items = details,
                IsPaid = false,
                OrderTotal = total,
                OrderNumber = $"LC-{DateTime.Today.Year}-{nextValue:D4}",
                OrderStatus = OrderStatus.Criado,
            };            

            await CreateAsync(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order created (unpaid) for {Username}.", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for {Username}.", username);
            return false;
        }
    }

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
    #endregion

    #region CRUD Orders
    public IQueryable<Order> GetAllOrders()
    {
        var orders = _context.Orders
            .Include(o => o.User)
            .AsQueryable();

        var user = _userHelper.GetCurrentUserAsync().Result; 
        var role = _userHelper.GetUserRoleAsync(user).Result;

        if (role == "Utente")
        {
            orders = orders.Where(o => o.User.Id == user.Id);
        }

        return orders;
    }

    public async Task<List<Order>> GetFilteredOrdersAsync(string? userId, string? orderNumber, DateTime? orderDate, DateTime? deliveryDate, DateTime? paymentDate, string? sortBy, bool sortDescending = false)
    {
        var orders = GetAllOrders();

        if (!string.IsNullOrEmpty(userId))
        {
            orders = orders.Where(o =>
            (o.User != null && o.User.Id == userId));
        }

        if (!string.IsNullOrEmpty(orderNumber))
        {
            orders = orders.Where(o =>
            o.OrderNumber != null &&
            EF.Functions.Like(o.OrderNumber, $"%{orderNumber}%"));
        }

        if (orderDate.HasValue)
        {
            orders = orders
                .Where(o => o.OrderDate >= orderDate.Value.Date && o.OrderDate < orderDate.Value.Date.AddDays(1));
        }

        if (deliveryDate.HasValue)
        {
            orders = orders
                .Where(o => o.DeliveryDate >= deliveryDate.Value.Date && o.DeliveryDate < deliveryDate.Value.Date.AddDays(1));
        }

        if (paymentDate.HasValue)
        {
            orders = orders
                .Where(o => o.PaymentDate >= paymentDate.Value.Date && o.PaymentDate < paymentDate.Value.Date.AddDays(1));
        }

        orders = sortBy switch
        {
            "OrderNumber" => sortDescending
                ? orders.OrderByDescending(o => o.OrderNumber)
                : orders.OrderBy(o => o.OrderNumber),

            "OrderDate" => sortDescending
                ? orders.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderDate)
                : orders.OrderBy(o => o.OrderDate).ThenBy(o => o.OrderDate),

            "DeliveryDate" => sortDescending
                ? orders.OrderByDescending(o => o.DeliveryDate).ThenByDescending(o => o.DeliveryDate)
                : orders.OrderBy(o => o.OrderDate).ThenBy(o => o.OrderDate),

            "PaymentDate" => sortDescending
                ? orders.OrderByDescending(o => o.PaymentDate).ThenByDescending(o => o.PaymentDate)
                : orders.OrderBy(o => o.PaymentDate).ThenBy(o => o.PaymentDate),

            "User" => sortDescending
                ? orders.OrderByDescending(o => o.User)
                : orders.OrderBy(o => o.User),

            "OrderTotal" => sortDescending
                ? orders.OrderByDescending(o => o.OrderTotal)
                : orders.OrderBy(o => o.OrderTotal),

            "IsPaid" => sortDescending
                ? orders.OrderByDescending(o => o.IsPaid)
                : orders.OrderBy(o => o.IsPaid),

            _ => orders.OrderBy(o => o.OrderDate).ThenBy(o => o.OrderDate)
        };

        return await orders.ToListAsync();
    }

    public async Task<int> GetRemainingCreditsAsync(string userId)
    {
        return await _context.Orders
        .Where(o => o.User.Id == userId && o.IsPaid)
        .SelectMany(o => o.Items)
        .SumAsync(i => (int?)i.RemainingUses ?? 0);
    }

    public async Task RefundCreditAsync(int orderDetailId)
    {
        var detail = await _context.OrderDetails.FindAsync(orderDetailId);
        if (detail != null)
        {
            detail.RemainingUses += 1;
            _context.OrderDetails.Update(detail);
            await _context.SaveChangesAsync();
        }
    }
    #endregion
}
