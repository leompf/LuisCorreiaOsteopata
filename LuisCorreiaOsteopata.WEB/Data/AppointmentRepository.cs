using AngleSharp.Dom;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace LuisCorreiaOsteopata.WEB.Data;

public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
{
    private readonly DataContext _context;
    private readonly ILogger<AppointmentRepository> _logger;
    private readonly IUserHelper _userHelper;

    public AppointmentRepository(DataContext context,
        ILogger<AppointmentRepository> logger,
        IUserHelper userHelper) : base(context)
    {
        _context = context;
        _logger = logger;
        _userHelper = userHelper;
    }

    #region CRUD Apppointments
    public IQueryable<Appointment> GetAllAppointments()
    {
        return _context.Appointments
            .Include(a => a.Staff)
                .ThenInclude(s => s.User)
            .Include(a => a.Patient)
                .ThenInclude(p => p.User);
    }

    public async Task<Appointment?> GetAppointmentByIdAsync(int? id)
    {
        return await _context.Appointments
           .Include(a => a.Patient)
            .ThenInclude(p => p.User)
           .Include(a => a.Staff)
            .ThenInclude(s => s.User)
           .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Appointment>> GetAppointmentsByUserAsync(User user)
    {
        var role = await _userHelper.GetUserRoleAsync(user);

        if (role == "Utente")
        {
            return await _context.Appointments
                         .Include(a => a.Staff)
                         .Include(a => a.Patient)
                         .Where(a => a.Patient.User.Id == user.Id)
                         .ToListAsync();
        }

        return await _context.Appointments
                         .Include(a => a.Staff)
                         .Include(a => a.Patient)
                         .Where(a => a.Staff.User.Id == user.Id)
                         .ToListAsync();
    }

    public async Task<List<Appointment>> GetFilteredAppointmentsAsync(string? userId, string? staffName, string? patientName, DateTime? fromDate, DateTime? toDate, string? sortBy, bool sortDescending = false)
    {
        var appointments = GetAllAppointments();

        if (!string.IsNullOrEmpty(userId))
        {
            appointments = appointments.Where(a =>
            (a.Staff != null && a.Staff.User.Id == userId) ||
            (a.Patient != null && a.Patient.User.Id == userId));
        }

        if (!string.IsNullOrEmpty(staffName))
        {
            appointments = appointments.Where(a =>
                a.Staff != null &&
                EF.Functions.Like(a.Staff.User.Names + " " + a.Staff.User.LastName, $"%{staffName}%"));
        }

        if (!string.IsNullOrEmpty(patientName))
        {
            appointments = appointments.Where(a =>
                a.Patient != null &&
                EF.Functions.Like(a.Patient.User.Names + " " + a.Patient.User.LastName, $"%{patientName}%"));
        }

        if (fromDate.HasValue)
        {
            appointments = appointments
                .Where(a => a.AppointmentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            appointments = appointments
                .Where(a => a.AppointmentDate <= toDate.Value);
        }

        appointments = sortBy switch
        {
            "Date" => sortDescending
                ? appointments.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime)
                : appointments.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime),

            "Patient" => sortDescending
                ? appointments.OrderByDescending(a => a.Patient.Names).ThenByDescending(a => a.Patient.LastName)
                : appointments.OrderBy(a => a.Patient.Names).ThenBy(a => a.Patient.LastName),

            "Staff" => sortDescending
                ? appointments.OrderByDescending(a => a.Staff.Names).ThenByDescending(a => a.Staff.LastName)
                : appointments.OrderBy(a => a.Staff.Names).ThenBy(a => a.Staff.LastName),

            "Status" => sortDescending
                ? appointments.OrderByDescending(a => a.AppointmentStatus)
                : appointments.OrderBy(a => a.AppointmentStatus),

            _ => appointments.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
        };

        return await appointments.ToListAsync();
    }

    public async Task<List<AppointmentViewModel>> GetSchedulledAppointmentsAsync()
    {
        var appointments =  GetAllAppointments();
        return await appointments.Select(a => new AppointmentViewModel
        {
            Id = a.Id,
            PatientId = a.PatientId,
            StaffId = a.StaffId,
            StaffName = a.Staff.FullName,
            StaffUserId = a.Staff.User.Id,
            AppointmentStatus = a.AppointmentStatus,
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            PatientNotes = a.PatientNotes,
            StaffNotes = a.StaffNotes,
        }).ToListAsync();
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateAppointmentAsync(Appointment appointment, int patientId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient?.User == null)
            {
                _logger.LogWarning("Invalid patient ID {PatientId} provided for appointment creation.", patientId);
                return (false, "Paciente inválido.");
            }

            var orderDetail = await _context.Orders
                .Where(o => o.User.Id == patient.User.Id && o.IsPaid)
                .SelectMany(o => o.Items)
                .Where(i => i.RemainingUses > 0)
                .OrderBy(i => i.Id)
                .FirstOrDefaultAsync();

            if (orderDetail == null)
            {
                _logger.LogInformation("Patient {PatientId} ({Email}) has no remaining appointment uses.", patientId, patient.User.Email);                
                return (false, "Não tem consultas disponíveis. Por favor, adquira novas consultas.");
            }

            orderDetail.RemainingUses--;
            appointment.OrderDetailId = orderDetail.Id;
            appointment.PatientId = patientId;
            appointment.AppointmentStatus = "Marcada";

            _context.Update(orderDetail);
            _context.Appointments.Add(appointment);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Appointment {AppointmentId} created successfully for patient {PatientId} ({Email}).", appointment.Id, patientId, patient.User.Email);
            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create appointment for patient {PatientId}.", patientId);
            return (false, $"Erro ao criar a consulta: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods
    public IEnumerable<SelectListItem> GetAvailableTimeSlotsCombo(DateTime date)
    {
        var now = DateTime.Now;

        if (date.Date < now.Date)
            return new List<SelectListItem>();

        (TimeOnly start, TimeOnly end)? workingHours = date.DayOfWeek switch
        {
            DayOfWeek.Monday or DayOfWeek.Tuesday or DayOfWeek.Wednesday
            or DayOfWeek.Thursday or DayOfWeek.Friday => (new TimeOnly(9, 0), new TimeOnly(19, 0)),

            DayOfWeek.Saturday => (new TimeOnly(9, 0), new TimeOnly(13, 0)),

            _ => null 
        };

        if (workingHours == null)
            return new List<SelectListItem>();

        var (startTime, endTime) = workingHours.Value;

        var bookedSlots = _context.Appointments
            .Where(a => a.AppointmentDate.Date == date.Date)
            .Select(a => a.StartTime)
            .ToList();

        var slotDuration = TimeSpan.FromMinutes(60);
        var availableSlots = new List<TimeOnly>();

        for (var t = startTime; t < endTime; t = t.AddMinutes(slotDuration.TotalMinutes))
        {
            if (bookedSlots.Contains(t))
                continue;

            if (date.Date == now.Date && t <= TimeOnly.FromDateTime(now))
                continue;

            availableSlots.Add(t);
        }

        return availableSlots.Select(t => new SelectListItem
        {
            Text = t.ToString("HH:mm"),
            Value = t.ToString("HH:mm")
        }).ToList();
    }
    #endregion
}
