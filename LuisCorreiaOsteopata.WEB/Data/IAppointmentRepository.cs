using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.WEB.Data;

public interface IAppointmentRepository : IGenericRepository<Appointment>
{
    Task<List<Appointment>> GetAllAppointmentsAsync();

    Task<List<Appointment>> GetFilteredAppointmentsAsync(string? userId ,string? staffName, string? patiendName, DateTime? fromDate, DateTime? toDate, string? sortBy, bool sortDescending);

    Task<List<AppointmentViewModel>> GetSchedulledAppointmentsAsync();

    Task<List<Appointment>> GetAppointmentsByUserAsync(User user);

    Task<Appointment?> GetAppointmentByIdAsync(int? id);

    IEnumerable<SelectListItem> GetAvailableTimeSlotsCombo(DateTime date);

}
