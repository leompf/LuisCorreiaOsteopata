using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
    public async Task<List<Appointment>> GetAllAppointmentsAsync()
    {
        return await _context.Appointments
            .Include(a => a.Staff)
                .ThenInclude(s => s.User)
            .Include(a => a.Patient)
                .ThenInclude(p => p.User)
            .ToListAsync();
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
        var appointments = await GetAllAppointmentsAsync();

        if (!string.IsNullOrEmpty(userId))
        {
            appointments = appointments
                .Where(a => (a.Staff != null && a.Staff.User.Id == userId)
                         || (a.Patient != null && a.Patient.User.Id == userId))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(staffName))
        {
            appointments = appointments
                .Where(a => a.Staff != null &&
                            a.Staff.FullName.Contains(staffName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(patientName))
        {
            appointments = appointments
                .Where(a => a.Patient != null &&
                            a.Patient.FullName.Contains(patientName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (fromDate.HasValue)
        {
            appointments = appointments
                .Where(a => a.AppointmentDate >= fromDate.Value)
                .ToList();
        }

        if (toDate.HasValue)
        {
            appointments = appointments
                .Where(a => a.AppointmentDate <= toDate.Value)
                .ToList();
        }

        appointments = sortBy switch
        {
            "Date" => sortDescending
                ? appointments.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime).ToList()
                : appointments.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).ToList(),

            "Patient" => sortDescending
                ? appointments.OrderByDescending(a => a.Patient.FullName).ToList()
                : appointments.OrderBy(a => a.Patient.FullName).ToList(),

            "Staff" => sortDescending
                ? appointments.OrderByDescending(a => a.Staff.FullName).ToList()
                : appointments.OrderBy(a => a.Staff.FullName).ToList(),

            "Status" => sortDescending
                ? appointments.OrderByDescending(a => a.AppointmentStatus).ToList()
                : appointments.OrderBy(a => a.AppointmentStatus).ToList(),

            _ => appointments.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).ToList()
        };

        return appointments;
    }

    public async Task<List<AppointmentViewModel>> GetSchedulledAppointmentsAsync()
    {
        var appointments = await GetAllAppointmentsAsync();
        return appointments.Select(a => new AppointmentViewModel
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
        }).ToList();
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

    #region Helper
    public IEnumerable<SelectListItem> GetAvailableTimeSlotsCombo(DateTime date)
    {
        var now = DateTime.Now;

        var booked = _context.Appointments
            .Where(a => a.AppointmentDate.Date == date.Date)
            .Select(a => a.StartTime)
            .ToList();

        TimeOnly start, end;

        if (date.DayOfWeek == DayOfWeek.Saturday)
        {
            start = new TimeOnly(9, 0);
            end = new TimeOnly(13, 0);
        }
        else if (date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday)
        {
            start = new TimeOnly(9, 0);
            end = new TimeOnly(19, 0);
        }
        else
        {
            return new List<SelectListItem>();
        }

        var slotDuration = TimeSpan.FromMinutes(60);
        var slots = new List<TimeOnly>();

        for (var t = start; t < end; t = t.AddMinutes(slotDuration.TotalMinutes))
        {
            if (booked.Contains(t))
                continue;

            if (date.Date == now.Date && t <= TimeOnly.FromDateTime(now))
                continue;

            slots.Add(t);
        }

        return slots.Select(t => new SelectListItem
        {
            Text = t.ToString("HH:mm"),
            Value = t.ToString("HH:mm")
        }).ToList();
    }
    #endregion
}
