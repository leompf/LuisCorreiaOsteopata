using LuisCorreiaOsteopata.Library.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace LuisCorreiaOsteopata.Library.Data;

public class StaffRepository : GenericRepository<Staff>, IStaffRepository
{
    private readonly DataContext _context;
    private readonly UserManager<User> _userManager;

    public StaffRepository(DataContext context, 
        UserManager<User> userManager) : base(context)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<Staff> CreatStaffAsync(User user, string roleName)
    {
        var isInrole = await _userManager.IsInRoleAsync(user, roleName);
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
}
