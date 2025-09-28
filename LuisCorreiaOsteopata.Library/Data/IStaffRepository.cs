using LuisCorreiaOsteopata.Library.Data.Entities;

namespace LuisCorreiaOsteopata.Library.Data;

public interface IStaffRepository : IGenericRepository<Staff>
{
    Task<Staff> CreatStaffAsync(User user, string roleName);
}
