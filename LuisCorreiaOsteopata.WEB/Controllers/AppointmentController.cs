using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.WEB.Controllers;

[Authorize]
public class AppointmentController : Controller
{
    private readonly IUserHelper _userHelper;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly IConverterHelper _converterHelper;
    private readonly IGoogleHelper _googleHelper;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AppointmentController> _logger;
    private readonly DataContext _context;

    public AppointmentController(IUserHelper userHelper,
        IAppointmentRepository appointmentRepository,
        IPatientRepository patientRepository,
        IStaffRepository staffRepository,
        IConverterHelper converterHelper,
        IGoogleHelper googleHelper,
        IEmailSender emailSender,
        ILogger<AppointmentController> logger,
        DataContext context)
    {
        _userHelper = userHelper;
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _staffRepository = staffRepository;
        _converterHelper = converterHelper;
        _googleHelper = googleHelper;
        _emailSender = emailSender;
        _logger = logger;
        _context = context;
    }


    #region CRUD Appointments
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userHelper.GetCurrentUserAsync();

        var model = new BookAppointmentViewModel
        {
            Staff = _staffRepository.GetComboStaff(),
            TimeSlots = _appointmentRepository.GetAvailableTimeSlotsCombo(DateTime.Today),
            Patients = _patientRepository.GetComboPatients()
        };

        if (User.IsInRole("Utente"))
        {
            var patient = await _patientRepository.GetPatientByUserEmailAsync(currentUser.Email);
            if (patient != null)
            {
                model.PatientId = patient.Id;
            }

            var remainingCredits = await _context.Orders
                .Where(o => o.User.Id == currentUser.Id && o.IsPaid)
                .SelectMany(o => o.Items)
                .SumAsync(i => i.RemainingUses);

            model.HasAvailableCredits = remainingCredits > 0;
            model.RemainingCredits = remainingCredits;
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(BookAppointmentViewModel model)
    {
        var currentUser = await _userHelper.GetCurrentUserAsync();

        model.TimeSlots = _appointmentRepository.GetAvailableTimeSlotsCombo(model.AppointmentDate);
        model.Staff = _staffRepository.GetComboStaff();
        if (User.IsInRole("Colaborador"))
            model.Patients = _patientRepository.GetComboPatients();

        if (!ModelState.IsValid)
        {
            ViewBag.AppointmentBookingMessage = "<span class='text-danger'>Ocorreu um erro ao marcar a consulta. Por favor, verifica se tens os dados corretos.</span>";
            return View(model);
        }

        var resolved = await ResolvePatientUserAsync(currentUser, model);
        if (resolved == null)
        {
            ModelState.AddModelError(string.Empty, "Paciente não encontrado.");
            return View(model);
        }
        var (patientUser, patientId) = resolved.Value;

        var staff = await _staffRepository.GetByIdAsync(model.StaffId);
        if (staff == null)
        {
            ModelState.AddModelError(string.Empty, "Colaborador selecionado não encontrado.");
            return View(model);
        }
        var staffUser = await ResolveStaffUserAsync(currentUser, staff);


        var endTime = model.StartTime.AddHours(1);

        var appointment = new Appointment
        {
            PatientId = patientId,
            StaffId = staff.Id,
            PatientNotes = model.Notes,
            AppointmentDate = model.AppointmentDate.Date,
            AppointmentStatus = "Marcada",
            StartTime = model.StartTime,
            EndTime = endTime
        };

        var result = await _appointmentRepository.CreateAppointmentAsync(appointment, patientId);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Erro ao criar a consulta.");
            return View(model);
        }

        await NotifyAppointmentCreatedAsync(appointment, (patientUser, patientId), staffUser, model.Notes);

        var message = @"
        <p>A consulta foi marcada com <strong>sucesso!</strong></p>
        <p>Podes visualizá-la no teu calendário da Homepage e ver os seus detalhes.</p>
        <p>Um email de confirmação foi enviado. Obrigado por contares connosco!</p>";

        ViewBag.AppointmentBookingMessage = _converterHelper.Sanitize(message);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id.Value);
        if (appointment == null)
            return NotFound();

        var model = _converterHelper.ToAppointmentViewModel(appointment);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id.Value);
        if (appointment == null)
            return NotFound();

        var model = new BookAppointmentViewModel
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            StaffId = appointment.StaffId,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            Notes = appointment.PatientNotes,
            Staff = _staffRepository.GetComboStaff(),
            Patients = _patientRepository.GetComboPatients(),
            TimeSlots = _appointmentRepository.GetAvailableTimeSlotsCombo(appointment.AppointmentDate)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BookAppointmentViewModel model)
    {
        model.TimeSlots = _appointmentRepository.GetAvailableTimeSlotsCombo(model.AppointmentDate);
        model.Staff = _staffRepository.GetComboStaff();
        if (User.IsInRole("Colaborador"))
            model.Patients = _patientRepository.GetComboPatients();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(model.Id);
        if (appointment == null) return NotFound();

        if (appointment.AppointmentDate != model.AppointmentDate ||
            appointment.StartTime != model.StartTime)
        {
            appointment.ReminderSent = false;
        }

        appointment.AppointmentDate = model.AppointmentDate;
        appointment.StartTime = model.StartTime;
        appointment.EndTime = model.StartTime.AddHours(1);
        appointment.StaffId = model.StaffId;
        appointment.PatientNotes = model.Notes;

        _context.Update(appointment);
        await _context.SaveChangesAsync();

        var message = @"
        <p>A consulta foi alterada com <strong>sucesso!</strong></p>
        <p>Podes visualizá-la no teu calendário da Homepage e ver os seus detalhes.</p>
        <p>Um email de confirmação foi enviado. Obrigado por contares connosco!</p>";

        ViewBag.AppointmentBookingMessage = _converterHelper.Sanitize(message);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }

        var googleEvents = await _context.GoogleCalendar
           .Where(e => e.AppointmentId == id)
           .ToListAsync();

        foreach (var evt in googleEvents)
        {
            var user = await _userHelper.GetUserByIdAsync(evt.UserId);
            try
            {
                await _googleHelper.DeleteEventAsync(user, evt.CalendarId, evt.EventId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to delete Google Calendar event {evt.EventId} for user {user.Email}");
            }
        }

        _context.GoogleCalendar.RemoveRange(googleEvents);
        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Account");
    }
    #endregion

    [Authorize(Roles = "Administrador,Colaborador")]
    [HttpGet("Appointment/ViewAppointments/{userId?}")]
    public async Task<IActionResult> ViewAppointments(string? userId, string? staffName, string? patientName, DateTime? fromDate, DateTime? toDate, string? sortBy = "Date", bool sortDescending = true)
    {
        var appointments = await _appointmentRepository.GetFilteredAppointmentsAsync(userId, staffName, patientName, fromDate, toDate, sortBy, sortDescending);

        var model = new AppointmentListViewModel
        {
            StaffName = staffName,
            PatientName = patientName,
            FromDate = fromDate,
            ToDate = toDate,
            Appointments = appointments.Select(a => _converterHelper.ToAppointmentViewModel(a))
        };

        ViewBag.DefaultSortBy = sortBy;
        ViewBag.DefaultSortDescending = sortDescending;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_AppointmentsTable", model.Appointments);

        return View(model);
    }

    #region Helper Methods
    private async Task<(User User, int PatientId)?> ResolvePatientUserAsync(User currentUser, BookAppointmentViewModel model)
    {
        if (User.IsInRole("Utente"))
        {
            var patient = await _patientRepository.GetPatientByUserEmailAsync(currentUser.Email);
            if (patient == null)
                return null;

            return (currentUser, patient.Id);
        }
        else if (User.IsInRole("Colaborador"))
        {
            var patientEntity = await _patientRepository.GetPatientByIdAsync(model.PatientId);
            if (patientEntity == null)
                return null;

            var user = await _userHelper.GetUserByEmailAsync(patientEntity.Email);

            return (user, patientEntity.Id);
        }

        return null;
    }

    private async Task<User> ResolveStaffUserAsync(User currentUser, Staff staff)
    {
        return User.IsInRole("Utente")
            ? await _userHelper.GetUserByEmailAsync(staff.Email)
            : currentUser;
    }

    private async Task NotifyAppointmentCreatedAsync(Appointment appointment, (User User, int PatientId) patient, User staffUser, string notes)
    {
        var startDateTime = appointment.AppointmentDate.Add(appointment.StartTime.ToTimeSpan());
        var endDateTime = appointment.AppointmentDate.Add(appointment.EndTime.ToTimeSpan());

        var usersToNotify = new[]
        {
        (currentUser: patient.User, role: "Utente", otherUser: staffUser),
        (currentUser: staffUser, role: "Colaborador", otherUser: patient.User)
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
                        <p>A tua consulta com <strong>{otherUser.Names} {otherUser.LastName}</strong> foi marcada para <strong>{formattedDate}</strong>.</p>
                        <p>Local: <a href=""https://maps.app.goo.gl/DNVAiw4m7ipYtE9h9"">Rua Camilo Castelo Branco, 2625-215 Póvoa de Santa Iria, Loja nº 27</a></p>
                        {(eventLink != null ? $"<p><a href='{eventLink}'>Ver no Google Calendar</a></p>" : "")}
                        <p>Obrigado por contares connosco!</p>"
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



