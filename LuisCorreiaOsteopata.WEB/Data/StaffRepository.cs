using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.WEB.Data;

public class StaffRepository : GenericRepository<Staff>, IStaffRepository
{
    private readonly DataContext _context;
    private readonly ILogger<StaffRepository> _logger;
    private readonly IUserHelper _userHelper;

    public StaffRepository(DataContext context,
        ILogger<StaffRepository> logger,
        IUserHelper userHelper) : base(context)
    {
        _context = context;
        _logger = logger;
        _userHelper = userHelper;
    }

    #region CRUD Staff
    public async Task<Staff> CreatStaffAsync(User user, string roleName)
    {
        _logger.LogInformation("Attempting to create staff for user {Email} with role {Role}.", user.Email, roleName);

        var isInrole = await _userHelper.IsUserInRoleAsync(user, roleName);
        if (!isInrole)
        {
            _logger.LogWarning("User {Email} is not in role {Role}, cannot create staff.", user.Email, roleName);
            return null;
        }

        var staff = new Staff
        {
            Names = user.Names,
            LastName = user.LastName,
            User = user,
            Email = user.Email,
            Nif = user.Nif,
            Phone = user.PhoneNumber,
        };

        _logger.LogInformation("Staff entity created for user {Email}.", user.Email);
        return staff;
    }

    public async Task<Staff?> GetStaffByUserEmailAsync(string email)
    {
        _logger.LogInformation("Fetching staff by user email {Email}.", email);

        var user = await _userHelper.GetUserByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("No user found with email {Email}.", email);
            return null;
        }

        var staff = await _context.Staff
            .FirstOrDefaultAsync(p => p.User.Id == user.Id);

        if (staff == null)
            _logger.LogWarning("No staff entity linked to user {Email}.", email);
        else
            _logger.LogInformation("Staff found for user {Email}: {FullName}.", email, staff.FullName);

        return staff;
    }
    #endregion

    #region Helper Methods
    public IEnumerable<SelectListItem> GetComboStaff()
    {
        _logger.LogInformation("Fetching combo list of staff.");

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

        _logger.LogInformation("{Count} staff members available in combo list.", list.Count);
        return list;
    }
    #endregion
}
