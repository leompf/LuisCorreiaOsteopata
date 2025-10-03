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

    public async Task<List<Appointment>> GetAllAppointmentsAsync()
    {
        return await _context.Appointments
            .Include(a => a.Staff)
            .Include(a => a.Patient)
            .ToListAsync();
    }

    public async Task<Appointment?> GetAppointmentByIdAsync(int? id)
    {
        return await _context.Appointments
           .Include(a => a.Patient)
           .Include(a => a.Staff)
           .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Appointment>> GetAppointmentsByUserAsync(User user)
    {
        return await _context.Appointments
                         .Include(a => a.Staff)
                         .Include(a => a.Patient)
                         .Where(a => a.Patient.User.Id == user.Id)
                         .ToListAsync();
    }

    public IEnumerable<SelectListItem> GetAvailableTimeSlotsCombo(DateTime date)
    {
        var booked = _context.Appointments
            .Where(a => a.AppointmentDate.Date == date.Date)
            .Select(a => TimeOnly.FromDateTime(a.StartTime))
            .ToList();

        var slots = new List<TimeOnly>();
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

        for (var t = start; t < end; t = t.AddMinutes(slotDuration.TotalMinutes))
        {
            if (!booked.Contains(t))
                slots.Add(t);
        }

        return slots.Select(t => new SelectListItem
        {
            Text = t.ToString("HH:mm"),
            Value = t.ToString("HH:mm")
        }).ToList();
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
}
