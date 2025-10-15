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

    public Task<bool> ConfirmOrderAsync(string username)
    {
        throw new NotImplementedException();
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

    public Task<IQueryable<Order>> GetOrderAsync(string userName)
    {
        throw new NotImplementedException();
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
