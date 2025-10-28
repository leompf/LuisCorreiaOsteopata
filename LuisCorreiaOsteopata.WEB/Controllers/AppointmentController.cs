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
            Patients = _patientRepository.GetComboPatients(),
            HasAvailableCredits = false
        };

        if (User.IsInRole("Utente"))
        {
            var patient = await _patientRepository.GetPatientByUserEmailAsync(currentUser.Email);
            if (patient != null)
            {
                model.PatientId = patient.Id;

                var remainingCredits = await _context.Orders
                    .Where(o => o.User.Id == currentUser.Id && o.IsPaid)
                    .SelectMany(o => o.Items)
                    .SumAsync(i => i.RemainingUses);

                model.HasAvailableCredits = remainingCredits > 0;
                model.RemainingCredits = remainingCredits;
            }
        }

        else if (User.IsInRole("Colaborador"))
        {
            model.Patients = _patientRepository.GetComboPatients();
            model.HasAvailableCredits = true;
            model.RemainingCredits = null;
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
        {
            model.Patients = _patientRepository.GetComboPatients();
        }
            
        if (!ModelState.IsValid)
        {
            return View(model);
        }
            
        var resolved = await ResolvePatientUserAsync(currentUser, model);
        if (resolved == null)
        {
            return View(model);
        }
            
        var (patientUser, patientId) = resolved.Value;

        var staff = await _staffRepository.GetByIdAsync(model.StaffId);
        if (staff == null)
        {
            return View(model);
        }
           
        var staffUser = await ResolveStaffUserAsync(currentUser, staff);

        var appointment = new Appointment
        {
            PatientId = patientId,
            StaffId = staff.Id,
            PatientNotes = model.Notes,
            AppointmentDate = model.AppointmentDate.Date,
            AppointmentStatus = "Marcada",
            StartTime = model.StartTime,
            EndTime = model.StartTime.AddHours(1)
        };

        string role;
        if (User.IsInRole("Utente"))
        {
            role = "Utente"; 
        }
        else if (User.IsInRole("Colaborador"))
        {
            role = "Colaborador"; 
        }
        else 
        {
            role = "Administrador"; 
        }

        var result = await _appointmentRepository.CreateAppointmentAsync(appointment, patientId, patientUser, role);

        ViewBag.AppointmentBookingMessageIsShown = true;
        ViewBag.AppointmentBookingMessage = _converterHelper.Sanitize(@"
            <p>A consulta foi marcada com <strong>sucesso!</strong></p>
            <p>Podes visualizá-la no teu calendário da Homepage e ver os seus detalhes.</p>
            <p>Um email de confirmação foi enviado. Obrigado por contares connosco!</p>
        ");

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
        if (appointment == null)
            return NotFound();

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

        var patientUser = await _userHelper.GetUserByEmailAsync(appointment.Patient.Email);
        var staffUser = await _userHelper.GetUserByEmailAsync(appointment.Staff.Email);

        var googleEvents = await _context.GoogleCalendar
        .Where(e => e.AppointmentId == appointment.Id)
        .ToListAsync();

        foreach (var evt in googleEvents)
        {
            var user = await _userHelper.GetUserByIdAsync(evt.UserId);
            try
            {
                var updatedEvent = await _googleHelper.UpdateEventAsync(
                    user,
                    evt.CalendarId,
                    evt.EventId,
                    $"Consulta com {appointment.Staff.Names} {appointment.Staff.LastName}",
                    appointment.PatientNotes,
                    appointment.AppointmentDate.Add(appointment.StartTime.ToTimeSpan()),
                    appointment.AppointmentDate.Add(appointment.EndTime.ToTimeSpan()),
                    CancellationToken.None
                );

                if (updatedEvent != null)
                {
                    evt.EventId = updatedEvent.Id; // In case the ID changed
                    _context.GoogleCalendar.Update(evt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update Google Calendar event {EventId} for user {UserId}", evt.EventId, user.Id);
            }
        }

        await _context.SaveChangesAsync();

        await NotifyAppointmentUpdatedAsync(appointment, patientUser, staffUser);

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
        var (success, errorMessage) = await _appointmentRepository.DeleteAppointmentAsync(id);

        if (!success)
        {
            _logger.LogWarning("Failed to delete appointment {AppointmentId}: {Error}", id, errorMessage);
            return BadRequest(errorMessage ?? "Erro ao cancelar a consulta.");
        }

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

    private async Task NotifyAppointmentUpdatedAsync(Appointment appointment, User patientUser, User staffUser)
    {
        var startDateTime = appointment.AppointmentDate.Add(appointment.StartTime.ToTimeSpan());

        try
        {
            var patientEmailBody = $@"
            <p>Olá {patientUser.Names.Split(' ')[0]},</p>
            <p>A tua consulta com <strong>{staffUser.Names} {staffUser.LastName}</strong> foi alterada para <strong>{startDateTime:f}</strong>.</p>
            <p>Obrigado por contares connosco!</p>";

            await _emailSender.SendEmailAsync(
                patientUser.Email!,
                "Alteração de Consulta",
                patientEmailBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send update email to patient {Email}.", patientUser.Email);
        }

        try
        {
            var staffEmailBody = $@"
            <p>Olá {staffUser.Names.Split(' ')[0]},</p>
            <p>A consulta com o paciente <strong>{patientUser.Names} {patientUser.LastName}</strong> foi alterada para <strong>{startDateTime:f}</strong>.</p>
            <p>Por favor, verifica o teu calendário e prepara a sessão.</p>";

            await _emailSender.SendEmailAsync(
                staffUser.Email!,
                "Alteração de Consulta",
                staffEmailBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send update email to staff {Email}.", staffUser.Email);
        }
    }
    #endregion
}



