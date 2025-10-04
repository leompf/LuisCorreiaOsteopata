using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuisCorreiaOsteopata.WEB.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly IUserHelper _userHelper;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IConverterHelper _converterHelper;
        private readonly DataContext _context;

        public AppointmentController(IUserHelper userHelper,
            IAppointmentRepository appointmentRepository,
            IPatientRepository patientRepository,
            IStaffRepository staffRepository,
            IConverterHelper converterHelper,
            DataContext context)
        {
            _userHelper = userHelper;
            _appointmentRepository = appointmentRepository;
            _patientRepository = patientRepository;
            _staffRepository = staffRepository;
            _converterHelper = converterHelper;
            _context = context;
        }

        [Authorize]
        public IActionResult Index()
        {
            var model = new BookAppointmentViewModel
            {
                Staff = _staffRepository.GetComboStaff(),
                TimeSlots = _appointmentRepository.GetAvailableTimeSlotsCombo(DateTime.Today)
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> Index(BookAppointmentViewModel model)
        {
            model.Staff = _staffRepository.GetComboStaff();

            if (!ModelState.IsValid)
            {
                model.Staff = _staffRepository.GetComboStaff();
                model.TimeSlots = _appointmentRepository.GetAvailableTimeSlotsCombo(model.AppointmentDate);
                return View(model);
            }

            var user = await _userHelper.GetCurrentUserAsync();

            var patient = await _patientRepository.GetPatientByUserEmailAsync(user.Email);
            if (patient == null)
            {
                ModelState.AddModelError(string.Empty, "Paciente não encontrado.");
                return View(model);
            }

            var staff = await _staffRepository.GetByIdAsync(model.StaffId);
            if (staff == null)
            {
                ModelState.AddModelError(string.Empty, "Colaborador selecionado não encontrado.");
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
                PatientId = patient.Id,
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

            var message =
                $"<p>A consulta foi marcada com <strong>sucesso!</strong></p>" +
                $"<p>Por favor, verifica o teu calendário na homepage e clica na consulta para confirmar os seus detalhes.</p>" +
                $"Obrigado por contares connosco!";

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
    }
}
