using LuisCorreiaOsteopata.Library.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace LuisCorreiaOsteopata.Library.Data;

public interface IStaffRepository : IGenericRepository<Staff>
{
    Task<Staff> CreatStaffAsync(User user, string roleName);

    Task<Staff?> GetStaffByUserEmailAsync(string email);

    IEnumerable<SelectListItem> GetComboStaff();
}
