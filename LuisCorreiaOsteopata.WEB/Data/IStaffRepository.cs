using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.WEB.Data;

public interface IStaffRepository : IGenericRepository<Staff>
{
    #region CRUD Staff
    Task<Staff> CreatStaffAsync(User user, string roleName);
    Task<Staff?> GetStaffByUserEmailAsync(string email);
    #endregion

    #region Helper Methods
    IEnumerable<SelectListItem> GetComboStaff();
    #endregion
}
