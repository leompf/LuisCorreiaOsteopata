using Ganss.Xss;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Models;
using System.Data;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public class ConverterHelper : IConverterHelper
{
    private readonly HtmlSanitizer _sanitizer;

    public ConverterHelper(HtmlSanitizer sanitizer)
    {
        _sanitizer = sanitizer;
    }

    public string Sanitize(string html)
    {
        return _sanitizer.Sanitize(html);
    }

    public AppointmentViewModel ToAppointmentViewModel(Appointment appointment)
    {
        return new AppointmentViewModel
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = appointment.Patient.FullName,
            StaffId = appointment.StaffId,
            StaffUserId = appointment.Staff.User.Id,
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

    public UserListViewModel ToUserListViewModel(User user, string role)
    {
        return new UserListViewModel
        {
            Id = user.Id,
            Name = $"{user.Names} {user.LastName}",
            Birthdate = user.Birthdate,
            Email = user.Email,
            NIF = user.Nif,
            PhoneNumber = user.PhoneNumber,
            Role = role
        };
    }
}
