using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Models;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public class ConverterHelper : IConverterHelper
{
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
            PatientNotes = appointment.PatientNotes,
            StaffNotes = appointment.StaffNotes,
            IsPaid = appointment.IsPaid
        };
    }
}
