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

    public async Task ModifyOrderDetailTempQuantityAsync(int id, double quantity)
    {
        var orderDetailTemp = await _context.OrderDetailsTemp.FindAsync(id);
        if (orderDetailTemp == null)
        {
            return;
        }

        orderDetailTemp.Quantity += quantity;
        if (orderDetailTemp.Quantity > 0)
        {
            _context.OrderDetailsTemp.Update(orderDetailTemp);
            await _context.SaveChangesAsync();
        }
    }
}
