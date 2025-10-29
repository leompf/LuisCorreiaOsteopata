using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.WEB.Data;

public interface IAppointmentRepository : IGenericRepository<Appointment>
{
    #region CRUD Appointments
    IQueryable<Appointment> GetAllAppointments();
    Task<List<Appointment>> GetFilteredAppointmentsAsync(string? userId, string? staffName, string? patiendName, DateTime? fromDate, DateTime? toDate, string? sortBy, bool sortDescending);
    Task<List<AppointmentViewModel>> GetSchedulledAppointmentsAsync(); //API
    Task<List<Appointment>> GetAppointmentsByUserAsync(User user);
    Task<Appointment?> GetAppointmentByIdAsync(int? id);
    Task<(bool Success, string? ErrorMessage)> CreateAppointmentAsync(Appointment appointment, int patientId, User currentUser, string role);
    Task<(bool Success, string? ErrorMessage)> DeleteAppointmentAsync(int appointmentId);
    #endregion

    #region Helper Methods
    IEnumerable<SelectListItem> GetAvailableTimeSlotsCombo(DateTime date);
    Task MarkPastAppointmentsAsCompletedAsync();
    #endregion
}
