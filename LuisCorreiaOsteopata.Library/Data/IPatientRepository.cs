using LuisCorreiaOsteopata.Library.Data.Entities;

namespace LuisCorreiaOsteopata.Library.Data;

public interface IPatientRepository : IGenericRepository<Patient>
{
    Task<Patient> CreatePatientAsync(User user, string roleName);

    Task<Patient?> GetPatientByUserEmailAsync(string email);
}
