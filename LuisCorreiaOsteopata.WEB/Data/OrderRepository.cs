using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.WEB.Data;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    private readonly DataContext _context;
    private readonly IUserHelper _userHelper;

    public OrderRepository(DataContext context,
        IUserHelper userHelper) : base(context)
    {
        _context = context;
        _userHelper = userHelper;
    }

    public async Task<bool> ConfirmOrderAsync(string username)
    {
        var user = await _userHelper.GetUserByEmailAsync(username);
        if (user == null)
        {
            return false;
        }

        var orderTmps = await _context.OrderDetailsTemp
            .Include(o => o.Product)
            .Where(o => o.User == user)
            .ToListAsync();

        if (orderTmps == null || orderTmps.Count == 0)
        {
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

        return true;
    }

    public async Task DeleteDetailTempAsync(int id)
    {
        var orderDetailTemp = await _context.OrderDetailsTemp.FindAsync(id);

        if (orderDetailTemp == null)
        {
            return;
        }

        _context.OrderDetailsTemp.Remove(orderDetailTemp);
        await _context.SaveChangesAsync();
    }

    public async Task<IQueryable<OrderDetailTemp>> GetDetailTempsAsync(string userName)
    {
        var user = await _userHelper.GetUserByEmailAsync(userName);

        if (user == null)
        {
            return null;
        }

        return _context.OrderDetailsTemp
            .Include(p => p.Product)
            .Where(o => o.User == user)
            .OrderByDescending(o => o.Product.Name);
    }

    public async Task<IQueryable<Order>> GetOrderAsync(string userName)
    {
        var user = await _userHelper.GetUserByEmailAsync(userName);

        if (user == null)
        {
            return null;
        }

        if (await _userHelper.IsUserInRoleAsync(user, "Admin"))
        {
            return _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(p => p.Product)
                .OrderByDescending(o => o.OrderDate);
        }

        return _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.User == user)
            .OrderByDescending(o => o.OrderDate);
    }   

    public async Task<(double itemTotal, double cartTotal, double newQuantity)?> ModifyOrderDetailTempQuantityAsync(int id, double quantity)
    {
        var orderDetail = await _context.OrderDetailsTemp
       .Include(o => o.Product)
       .FirstOrDefaultAsync(o => o.Id == id);

        if (orderDetail == null)
            return null;

        orderDetail.Quantity += quantity;
        if (orderDetail.Quantity < 1)
            orderDetail.Quantity = 1;

        _context.OrderDetailsTemp.Update(orderDetail);
        await _context.SaveChangesAsync();

        var cartTotal = await _context.OrderDetailsTemp
            .SumAsync(i => (double)i.Price * i.Quantity);

        var itemTotal = (double)orderDetail.Price * orderDetail.Quantity;

        return (itemTotal, cartTotal, orderDetail.Quantity);
    }
}
