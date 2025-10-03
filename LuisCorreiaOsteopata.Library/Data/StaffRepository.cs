using LuisCorreiaOsteopata.Library.Data.Entities;
using LuisCorreiaOsteopata.Library.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


namespace LuisCorreiaOsteopata.Library.Data;

public class StaffRepository : GenericRepository<Staff>, IStaffRepository
{
    private readonly DataContext _context;
    private readonly IUserHelper _userHelper;

    public StaffRepository(DataContext context, 
        IUserHelper userHelper) : base(context)
    {
        _context = context;
       _userHelper = userHelper;
    }

    public async Task<Staff> CreatStaffAsync(User user, string roleName)
    {
        var isInrole = await _userHelper.IsUserInRoleAsync(user, roleName);
        if (!isInrole)
        {
            return null;
        }

        var staff = new Staff
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            User = user,
            Email = user.Email,
            Nif = user.Nif,
            Phone = user.PhoneNumber,
        };

        return staff;
    }

    public IEnumerable<SelectListItem> GetComboStaff()
    {
        var list = _context.Staff.Select(s => new SelectListItem
        {
            Text = s.FullName,
            Value = s.Id.ToString(),
        }).ToList();

        list.Insert(0, new SelectListItem
        {
            Text = "(Seleciona um profissional...)",
            Value = "0"
        });

        return list;
    }

    public async Task<Staff?> GetStaffByUserEmailAsync(string email)
    {
        var user = await _userHelper.GetUserByEmailAsync(email);
        if (user == null)
        {
            return null;
        }

        return await _context.Staff
            .FirstOrDefaultAsync(p => p.User.Id == user.Id);
    }
}
