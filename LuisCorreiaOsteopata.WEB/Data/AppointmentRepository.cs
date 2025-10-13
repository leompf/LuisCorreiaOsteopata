using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.WEB.Data;

public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
{
    private readonly DataContext _context;
    private readonly IUserHelper _userHelper;

    public AppointmentRepository(DataContext context,
        IUserHelper userHelper) : base(context)
    {
        _context = context;
        _userHelper = userHelper;
    }

    public async Task AddAppointmentCreditsAsync(string userId, int quantity)
    {
        var user = await _userHelper.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ArgumentException("User not found.", nameof(userId));
        }

        var credit = new AppointmentCredit
        {
            UserId = user.Id,
            TotalAppointments = quantity,
            UsedAppointments = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.AppointmentCredits.Add(credit);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ConsumeAppointmentCreditAsync(int patientId)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null)
            return false;

        var credit = await _context.AppointmentCredits
            .FirstOrDefaultAsync(c =>
                c.UserId == patient.User.Id &&
                c.UsedAppointments < c.TotalAppointments);

        if (credit == null)
            return false;

        credit.UsedAppointments += 1;
        _context.AppointmentCredits.Update(credit);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateAppointmentAsync(Appointment appointment, int patientId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
                return (false, "Paciente inválido.");

            var credit = await _context.AppointmentCredits
                .FirstOrDefaultAsync(c =>
                    c.UserId == patient.User.Id &&
                    c.UsedAppointments < c.TotalAppointments);

            if (credit == null)
                return (false, "Sem créditos disponíveis para agendamento.");

            credit.UsedAppointments += 1;
            _context.AppointmentCredits.Update(credit);

            appointment.CreatedDate = DateTime.UtcNow;
            appointment.AppointmentStatus = "Marcada";

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Erro ao criar a consulta: {ex.Message}");
        }
    }
    

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

    public IEnumerable<SelectListItem> GetAvailableTimeSlotsCombo(DateTime date)
    {
        var now = DateTime.Now;

        var booked = _context.Appointments
            .Where(a => a.AppointmentDate.Date == date.Date)
            .Select(a => TimeOnly.FromDateTime(a.StartTime))
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

        // Apply name filters for real-time search
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

        // Sorting
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
            CreatedDate = a.CreatedDate,
            AppointmentStatus = a.AppointmentStatus,
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            PatientNotes = a.PatientNotes,
            StaffNotes = a.StaffNotes,
            IsPaid = a.IsPaid,
        }).ToList();
    }

    public async Task<bool> HasAvailableCreditsAsync(string userId)
    {
        var hasCredits = await _context.AppointmentCredits
            .AnyAsync(a => a.UserId == userId && a.UsedAppointments < a.TotalAppointments);

        return hasCredits;
    }
}
