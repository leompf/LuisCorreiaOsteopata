using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.WEB.Data;

public class PatientRepository : GenericRepository<Patient>, IPatientRepository
{
    private readonly DataContext _context;
    private readonly ILogger<PatientRepository> _logger;
    private readonly IUserHelper _userHelper;

    public PatientRepository(DataContext context,
        ILogger<PatientRepository> logger,
        IUserHelper userHelper) : base(context)
    {
        _context = context;
        _logger = logger;
        _userHelper = userHelper;
    }

    #region CRUD Patients
    public async Task<Patient?> GetPatientByIdAsync(int id)
    {
        _logger.LogInformation("Fetching patient by ID {PatientId}.", id);

        var patient = await _context.Patients
            .Include(p => p.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null)
            _logger.LogWarning("No patient found with ID {PatientId}.", id);
        else
            _logger.LogInformation("Patient found: {FullName}.", patient.FullName);

        return patient;
    }

    public async Task<Patient?> GetPatientByUserEmailAsync(string email)
    {
        _logger.LogInformation("Fetching patient by user email {Email}.", email);

        var user = await _userHelper.GetUserByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("No user found with email {Email}.", email);
            return null;
        }

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.User.Id == user.Id);

        if (patient == null)
            _logger.LogWarning("No patient entity linked to user {Email}.", email);
        else
            _logger.LogInformation("Patient found for user {Email}: {FullName}.", email, patient.FullName);

        return patient;
    }

    public async Task<Patient> CreatePatientAsync(User user, string roleName)
    {
        _logger.LogInformation("Attempting to create patient for user {Email} with role {Role}.", user.Email, roleName);

        var isInRole = await _userHelper.IsUserInRoleAsync(user, roleName);

        if (!isInRole)
        {
            _logger.LogWarning("User {Email} is not in role {Role}, cannot create patient.", user.Email, roleName);
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

        _logger.LogInformation("Patient entity created for user {Email}.", user.Email);
        return patient;
    }
    #endregion

    #region Helper Methods
    public IEnumerable<SelectListItem> GetComboPatients()
    {
        _logger.LogInformation("Fetching combo list of patients.");

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

        _logger.LogInformation("{Count} patients available in combo list.", list.Count);
        return list;
    }
    #endregion
}
