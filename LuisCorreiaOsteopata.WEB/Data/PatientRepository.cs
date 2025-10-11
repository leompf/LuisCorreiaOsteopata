using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.WEB.Data;

public class PatientRepository : GenericRepository<Patient>, IPatientRepository
{
    private readonly DataContext _context;
    private readonly IUserHelper _userHelper;

    public PatientRepository(DataContext context,
        IUserHelper userHelper) : base(context)
    {
        _context = context;
        _userHelper = userHelper;
    }


    public async Task<Patient> CreatePatientAsync(User user, string roleName)
    {
        var isInRole = await _userHelper.IsUserInRoleAsync(user, roleName);

        if (!isInRole)
        {
            return null;
        }

        var patient = new Patient
        {
            Names = user.Names,
            LastName = user.LastName,
            User = user,
            Email = user.Email,
            Nif = user.Nif,
            Phone = user.PhoneNumber,
        };

        return patient;
    }

    public IEnumerable<SelectListItem> GetComboPatients()
    {
        var list = _context.Patients.Select(s => new SelectListItem
        {
            Text = s.FullName,
            Value = s.Id.ToString(),
        }).ToList();

        list.Insert(0, new SelectListItem
        {
            Text = "(Seleciona um paciente...)",
            Value = null //Alterado para ver se os filtros funcionam na pagina com todas as consultas. Original "0"
        });

        return list;
    }

    public async Task<Patient?> GetPatientByIdAsync(int id)
    {
        return await _context.Patients
                        .Include(p => p.User)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Patient?> GetPatientByUserEmailAsync(string email)
    {
        var user = await _userHelper.GetUserByEmailAsync(email);
        if (user == null)
        {
            return null;
        }

        return await _context.Patients
            .FirstOrDefaultAsync(p => p.User.Id == user.Id);
    }
}
