using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    public async Task<IActionResult> Index()
    {
        var model = new BookAppointmentViewModel
        {
            Staff = _staffRepository.GetComboStaff(),
            TimeSlots = _appointmentRepository.GetAvailableTimeSlotsCombo(DateTime.Today),
            Patients = _patientRepository.GetComboPatients()
        };

        if (User.IsInRole("Utente"))
        {
            var user = await _userHelper.GetCurrentUserAsync();
            var patient = await _patientRepository.GetPatientByUserEmailAsync(user.Email);
            if (patient != null)
            {
                model.PatientId = patient.Id;
            }
        }

        return View(model);
    }


    [HttpPost]
    public async Task<IActionResult> Index(BookAppointmentViewModel model)
    {
        if (User.IsInRole("Colaborador"))
            model.Patients = _patientRepository.GetComboPatients();

        if (!ModelState.IsValid)
        {
            ViewBag.AppointmentBookingMessage = "<span class='text-danger'>Ocorreu um erro ao marcar a consulta. Por favor, verifica se tens os dados corretos.</span>";
            model.TimeSlots = _appointmentRepository.GetAvailableTimeSlotsCombo(model.AppointmentDate);
            model.Staff = _staffRepository.GetComboStaff();
            return View(model);
        }

        var user = await _userHelper.GetCurrentUserAsync();
        int patientId;

        if (User.IsInRole("Utente"))
        {
            var patient = await _patientRepository.GetPatientByUserEmailAsync(user.Email);
            if (patient == null)
            {
                ModelState.AddModelError(string.Empty, "Paciente não encontrado.");
                model.TimeSlots = _appointmentRepository.GetAvailableTimeSlotsCombo(model.AppointmentDate);
                return View(model);
            }
            patientId = patient.Id;
        }
        else if (User.IsInRole("Colaborador"))
        {
            if (model.PatientId == null)
            {
                ModelState.AddModelError(string.Empty, "Paciente não encontrado.");
                model.TimeSlots = _appointmentRepository.GetAvailableTimeSlotsCombo(model.AppointmentDate);
                return View(model);
            }
            patientId = model.PatientId;
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Não é possível marcar a consulta.");
            return View(model);
        }

        var staff = await _staffRepository.GetByIdAsync(model.StaffId);
        if (staff == null)
        {
            ModelState.AddModelError(string.Empty, "Colaborador selecionado não encontrado.");
            model.TimeSlots = _appointmentRepository.GetAvailableTimeSlotsCombo(model.AppointmentDate);
            return View(model);
        }

        if (!TimeOnly.TryParse(model.StartTime, out var startTime))
        {
            ModelState.AddModelError(string.Empty, "Horário inválido.");
            model.TimeSlots = _appointmentRepository.GetAvailableTimeSlotsCombo(model.AppointmentDate);
            return View(model);
        }

        var endTime = startTime.AddHours(1);

        var startDateTime = model.AppointmentDate.Date
                            .AddHours(startTime.Hour)
                            .AddMinutes(startTime.Minute);

        var endDateTime = model.AppointmentDate.Date
                          .AddHours(endTime.Hour)
                          .AddMinutes(endTime.Minute);

        var appointment = new Appointment
        {
            PatientId = patientId,
            StaffId = staff.Id,
            PatientNotes = model.Notes,
            AppointmentDate = model.AppointmentDate.Date,
            CreatedDate = DateTime.Now,
            AppointmentStatus = "Marcada",
            IsPaid = false,
            StartTime = startDateTime,
            EndTime = endDateTime
        };

        await _appointmentRepository.CreateAsync(appointment);
        await _context.SaveChangesAsync();

        try
        {
            User patientUser;
            User staffUser;

            if (User.IsInRole("Utente"))
            {
                patientUser = user;
                staffUser = await _userHelper.GetUserByEmailAsync(staff.Email);
            }
            else
            {
                staffUser = user;
                var patientEntity = await _patientRepository.GetPatientByIdAsync(patientId);
                patientUser = await _userHelper.GetUserByEmailAsync(patientEntity.User.Email);
            }

            foreach (var (currentUser, role, otherUser) in new[]
                 {
                     (patientUser, "Utente", staffUser),
                     (staffUser, "Colaborador", patientUser)
                 })
            {
                try
                {
                    var createdEvent = await _googleHelper.CreateEventAsync(
                        currentUser,
                        currentUser.CalendarId,
                        $"Consulta com {otherUser.Names} {otherUser.LastName}",
                        model.Notes,
                        startDateTime,
                        endDateTime,
                        CancellationToken.None
                    );

                    string? eventLink = null;
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
                        eventLink = createdEvent.HtmlLink;
                    }

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

                    await _emailSender.SendEmailAsync(currentUser.Email,
                        role == "Utente" ? "Consulta Marcada com Sucesso" : "Nova Consulta Marcada",
                        emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create Google Calendar events or send emails.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error while creating Google Calendar events or sending emails.");
        }

        var message =
            $"<p>A consulta foi marcada com <strong>sucesso!</strong></p>" +
            $"<p>Podes agora visualizá-la no teu calendário da Homepage. Clica na mesma para ver os seus detalhes.</p>" +
            $"Foi enviado um email de confirmação. Obrigado por contares connosco!";

        ViewBag.AppointmentBookingMessage = _converterHelper.Sanitize(message);

        return View(model);
    }


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


    [HttpPost]
    public async Task<IActionResult> Unbook(int id)
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


    [Authorize(Roles = "Administrador,Colaborador")]
    public async Task<IActionResult> ViewAppointments(int? staffId, int? patientId,DateTime? fromDate, DateTime? toDate)
    {
        var appointments = await _appointmentRepository.GetFilteredAppointmentsAsync(staffId, patientId, fromDate, toDate);
        var users = await _userHelper.GetAllUsersAsync();

        var model = new AppointmentListViewModel
        {
            StaffId = staffId,
            PatientId = patientId,
            FromDate = fromDate,
            ToDate = toDate,
            StaffMembers = _staffRepository.GetComboStaff(),
            Patients = _patientRepository.GetComboPatients(),
            Appointments = appointments.Select(a => _converterHelper.ToAppointmentViewModel(a))
        };

        return View(model);
    }
}
