using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Models;

using Ganss.Xss;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public class ConverterHelper : IConverterHelper
{
    private readonly HtmlSanitizer _sanitizer;

    public ConverterHelper(HtmlSanitizer sanitizer)
    {
        _sanitizer = sanitizer;
    }
    public AppointmentViewModel ToAppointmentViewModel(Appointment appointment)
    {
        return new AppointmentViewModel
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = appointment.Patient.FullName,
            StaffId = appointment.StaffId,
            StaffName = appointment.Staff.FullName,
            CreatedDate = appointment.CreatedDate,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            AppointmentStatus = appointment.AppointmentStatus,
            PatientNotes = _sanitizer.Sanitize(appointment.PatientNotes),
            StaffNotes = _sanitizer.Sanitize(appointment.StaffNotes),
            IsPaid = appointment.IsPaid
        };
    }
}
