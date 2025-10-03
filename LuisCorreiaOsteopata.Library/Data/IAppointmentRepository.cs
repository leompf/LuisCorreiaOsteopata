using LuisCorreiaOsteopata.Library.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.Library.Data;

public interface IAppointmentRepository : IGenericRepository<Appointment>
{
    Task<List<Appointment>> GetAllAppointmentsAsync();

    Task<List<AppointmentDto>> GetSchedulledAppointmentsAsync();

    Task<List<Appointment>> GetAppointmentsByUserAsync(User user);

    IEnumerable<SelectListItem> GetAvailableTimeSlotsCombo(DateTime date);

}
