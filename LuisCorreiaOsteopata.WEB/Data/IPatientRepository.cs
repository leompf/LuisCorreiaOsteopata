using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.WEB.Data;

public interface IPatientRepository : IGenericRepository<Patient>
{
    Task<Patient> CreatePatientAsync(User user, string roleName);

    Task<Patient?> GetPatientByUserEmailAsync(string email);

    IEnumerable<SelectListItem> GetComboPatients();
}
