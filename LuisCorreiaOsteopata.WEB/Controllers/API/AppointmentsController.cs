using LuisCorreiaOsteopata.Library.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuisCorreiaOsteopata.WEB.Controllers.API;

[Route("api/[controller]")]
[ApiController]
public class AppointmentsController : Controller
{
    private readonly IAppointmentRepository _appointmentRepository;

    public AppointmentsController(IAppointmentRepository appointmentRepository)
    {
        _appointmentRepository = appointmentRepository;
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public IActionResult GetAllAppointments()
    {
        var appointments = _appointmentRepository.GetSchedulledAppointmentsAsync();
        return Ok(appointments);
    }


    [HttpGet("AvailableTimeSlots")]
    public IActionResult GetAvailableTimeSlots([FromQuery] DateTime date)
    {
        var slots = _appointmentRepository.GetAvailableTimeSlotsCombo(date);
        return Ok(slots);
    }
}
