using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.WEB.Data;

public interface IPatientRepository : IGenericRepository<Patient>
{
    #region CRUD Patients
    Task<Patient> CreatePatientAsync(User user, string roleName);
    Task<Patient?> GetPatientByUserEmailAsync(string email);
    Task<Patient?> GetPatientByIdAsync(int id);
    #endregion

    #region Helper Methods
    IEnumerable<SelectListItem> GetComboPatients();
    #endregion
}
