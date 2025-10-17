using LuisCorreiaOsteopata.WEB.Data;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace LuisCorreiaOsteopata.WEB.Helpers
{
    public class ReminderHelper : IReminderHelper
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IEmailSender _emailSender;

        public ReminderHelper(IAppointmentRepository appointmentRepository, 
            IEmailSender emailSender)
        {
            _appointmentRepository = appointmentRepository;
            _emailSender = emailSender;
        }
        public async Task SendAppointmentReminderAsync()
        {
            var targetDate = DateTime.UtcNow.AddHours(24).Date;

            var appointments = await _appointmentRepository.GetAllAppointmentsAsync();
            var upcomingAppointments = appointments
                .Where(a => a.AppointmentDate.Date == targetDate)
                .ToList();

            foreach (var appointment in upcomingAppointments)
            {
                if (appointment.Patient?.User?.Email != null)
                {
                    var message = $"Olá {appointment.Patient.FullName},\n\n" +
                                  $"Este é um lembrete da sua consulta com {appointment.Staff.FullName} " +
                                  $"no dia {appointment.AppointmentDate:dd/MM/yyyy} às {appointment.StartTime:HH:mm}.\n\n" +
                                  "Obrigado!";

                    await _emailSender.SendEmailAsync(
                        appointment.Patient.User.Email,
                        "Lembrete de Consulta",
                        message
                    );
                }
            }
        }
    }
}
