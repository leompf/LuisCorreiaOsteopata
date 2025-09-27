using LuisCorreiaOsteopata.Library.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace LuisCorreiaOsteopata.Library.Data;

public class PatientRepository : GenericRepository<Patient>, IPatientRepository
{
    private readonly DataContext _context;
    private readonly UserManager<User> _userManager;

    public PatientRepository(DataContext context,
        UserManager<User> userManager) : base(context)
    {
        _context = context;
        _userManager = userManager;
    }


    public async Task<Patient> CreatePatientAsync(User user, string roleName)
    {
        var isInrole = await _userManager.IsInRoleAsync(user, roleName);
        if (!isInrole)
        {
            return null;
        }

        var patient = new Patient
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            User = user,
        };

        return patient;
    }
}
