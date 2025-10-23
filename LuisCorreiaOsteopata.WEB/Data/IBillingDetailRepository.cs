using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.WEB.Data
{
    public interface IBillingDetailRepository : IGenericRepository<BillingDetail>
    {
        Task<List<BillingDetail>> GetBillingDetailsByUserAsync(string username, int limit = 3);
    }
}
