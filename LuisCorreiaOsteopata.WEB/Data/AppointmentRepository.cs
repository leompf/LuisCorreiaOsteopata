using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.WEB.Data;

public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
{
    private readonly DataContext _context;
    private readonly ILogger<AppointmentRepository> _logger;
    private readonly IGoogleHelper _googleHelper;
    private readonly IEmailSender _emailSender;
    private readonly IUserHelper _userHelper;

    public AppointmentRepository(DataContext context,
        ILogger<AppointmentRepository> logger,
        IGoogleHelper googleHelper,
        IEmailSender emailSender,
        IUserHelper userHelper) : base(context)
    {
        _context = context;
        _logger = logger;
        _googleHelper = googleHelper;
        _emailSender = emailSender;
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
        var appointments = GetAllAppointments();
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

    public async Task<(bool Success, string? ErrorMessage)> CreateAppointmentAsync(Appointment appointment, int patientId, User currentUser, string role)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            Patient? patient;

            if (role == "Utente")
            {
                patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.User.Id == currentUser.Id);

                if (patient == null)
                {
                    _logger.LogWarning("No patient found for user {UserId}", currentUser.Id);
                    return (false, "Paciente inválido.");
                }

                patientId = patient.Id; 
            }
            else 
            {
                patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == patientId);

                if (patient == null)
                {
                    _logger.LogWarning("Invalid patient ID {PatientId} provided by collaborator", patientId);
                    return (false, "Paciente inválido.");
                }
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

            await NotifyAppointmentCreatedAsync(appointment, patient.User, appointment.Staff.User, appointment.PatientNotes);

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

    public async Task<(bool Success, string? ErrorMessage)> DeleteAppointmentAsync(int appointmentId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient.User)
                .Include(a => a.OrderDetail)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                _logger.LogWarning("Attempted to delete non-existent appointment ID {AppointmentId}", appointmentId);
                return (false, "Consulta não encontrada.");
            }

            var appointmentDateTime = appointment.AppointmentDate.Add(appointment.StartTime.ToTimeSpan());
            var hoursUntilAppointment = (appointmentDateTime - DateTime.Now).TotalHours;
            _logger.LogInformation("Appointment {AppointmentId} is {HoursUntil:F2} hours away", appointmentId, hoursUntilAppointment);

            if (hoursUntilAppointment > 24 && appointment.OrderDetail != null)
            {
                appointment.OrderDetail.RemainingUses += 1;
                _context.OrderDetails.Update(appointment.OrderDetail);
                _logger.LogInformation("Refunded 1 credit for order detail {OrderDetailId}", appointment.OrderDetail.Id);
            }

            var googleEvents = await _context.GoogleCalendar
                .Where(e => e.AppointmentId == appointmentId)
                .ToListAsync();

            _logger.LogInformation("Found {EventCount} Google events linked to appointment {AppointmentId}", googleEvents.Count, appointmentId);

            foreach (var evt in googleEvents)
            {
                var user = await _userHelper.GetUserByIdAsync(evt.UserId);
                try
                {
                    await _googleHelper.DeleteEventAsync(user, evt.CalendarId, evt.EventId, CancellationToken.None);
                    _logger.LogInformation("Deleted Google Calendar event {EventId}", evt.EventId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete Google Calendar event {EventId}", evt.EventId);
                }
            }

            _context.GoogleCalendar.RemoveRange(googleEvents);
            _context.Appointments.Remove(appointment);

            var affected = await _context.SaveChangesAsync();
            _logger.LogInformation("SaveChanges affected {Count} entities", affected);

            await transaction.CommitAsync();
            _logger.LogInformation("Appointment {AppointmentId} successfully deleted", appointment.Id);

            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error deleting appointment {AppointmentId}", appointmentId);
            return (false, "Erro ao cancelar a consulta. Por favor, tente novamente.");
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

    private async Task NotifyAppointmentCreatedAsync(Appointment appointment, User patientUser, User staffUser, string notes)
    {
        var startDateTime = appointment.AppointmentDate.Add(appointment.StartTime.ToTimeSpan());
        var endDateTime = appointment.AppointmentDate.Add(appointment.EndTime.ToTimeSpan());

        var usersToNotify = new[]
        {
        (currentUser: patientUser, role: "Utente", otherUser: staffUser),
        (currentUser: staffUser, role: "Colaborador", otherUser: patientUser)
    };

        foreach (var (currentUser, role, otherUser) in usersToNotify)
        {
            string? eventLink = null;

            try
            {
                if (!string.IsNullOrEmpty(currentUser.CalendarId))
                {
                    var createdEvent = await _googleHelper.CreateEventAsync(
                        currentUser,
                        currentUser.CalendarId,
                        $"Consulta com {otherUser.Names} {otherUser.LastName}",
                        notes,
                        startDateTime,
                        endDateTime,
                        CancellationToken.None
                    );

                    eventLink = createdEvent?.HtmlLink;

                    if (createdEvent != null)
                    {
                        _context.GoogleCalendar.Add(new GoogleCalendar
                        {
                            AppointmentId = appointment.Id,
                            UserId = currentUser.Id,
                            EventId = createdEvent.Id,
                            CalendarId = currentUser.CalendarId,
                            Role = role
                        });
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google Calendar sync failed for user {UserId}, continuing with email.", currentUser.Id);
            }

            try
            {
                var formattedDate = startDateTime.ToString("f", new System.Globalization.CultureInfo("pt-PT"));
                var emailBody = role == "Utente"
                    ? $@"
                    <h3>Consulta Marcada com Sucesso</h3>
                    <p>Olá {currentUser.Names.Split(' ')[0]},</p>
                    <p>A tua consulta com <strong>{otherUser.Names} {otherUser.LastName}</strong> foi marcada para <strong>{formattedDate}</strong>.<br />
                    Local: <a href=""https://maps.app.goo.gl/DNVAiw4m7ipYtE9h9"">Rua Camilo Castelo Branco, 2625-215 Póvoa de Santa Iria, Loja nº 27</a></p>
                    {(eventLink != null ? $"<p><a href='{eventLink}'>Ver no Google Calendar</a></p>" : "")}
                    <p>Obrigado e cumprimentos,<br />
                    Luís Correia, Osteopata</p>"
                    : $@"
                    <h3>Nova Consulta Marcada</h3>
                    <p>Olá {currentUser.Names.Split(' ')[0]},</p>
                    <p>Foi marcada uma nova consulta com o paciente <strong>{otherUser.Names} {otherUser.LastName}</strong> para <strong>{formattedDate}</strong>.</p>
                    {(eventLink != null ? $"<p><a href='{eventLink}'>Ver no Google Calendar</a></p>" : "")}
                    <p>Por favor, verifica o teu calendário e prepara a sessão.</p>";

                await _emailSender.SendEmailAsync(
                    currentUser.Email!,
                    role == "Utente" ? "Consulta Marcada com Sucesso" : "Nova Consulta Marcada",
                    emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send appointment email to {Email}.", currentUser.Email);
            }
        }
    }
    #endregion
}
