using LuisCorreiaOsteopata.Library.Data;
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

    [HttpGet]
    public async Task<IActionResult> GetAppointments()
    {
        var appointments = await _appointmentRepository.GetSchedulledAppointmentsAsync();
        return Ok(appointments);
    }

    [HttpGet("AvailableTimeSlots")]
    public IActionResult GetAvailableTimeSlots([FromQuery] DateTime date)
    {
        var slots = _appointmentRepository.GetAvailableTimeSlotsCombo(date);
        return Ok(slots);
    }
}
