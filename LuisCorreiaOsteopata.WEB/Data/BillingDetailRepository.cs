using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.WEB.Data
{
    public class BillingDetailRepository : GenericRepository<BillingDetail>, IBillingDetailRepository
    {
        private readonly DataContext _context;

        public BillingDetailRepository(DataContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<BillingDetail>> GetBillingDetailsByUserAsync(string username, int limit = 3)
        {
            return await _context.BillingDetails
                .Where(b => b.User.UserName == username)
                .Take(limit)
                .ToListAsync();
        }
    }
}
