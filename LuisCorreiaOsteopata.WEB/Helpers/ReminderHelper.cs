using LuisCorreiaOsteopata.WEB.Data;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace LuisCorreiaOsteopata.WEB.Helpers
{
    public class ReminderHelper : IReminderHelper
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IEmailSender _emailSender;
        private readonly DataContext _context;

        public ReminderHelper(IAppointmentRepository appointmentRepository, 
            IEmailSender emailSender,
            DataContext context)
        {
            _appointmentRepository = appointmentRepository;
            _emailSender = emailSender;
            _context = context;
        }
        public async Task SendAppointmentReminderAsync()
        {
            var targetDate = DateTime.UtcNow.AddHours(24).Date;

            var appointments = await _appointmentRepository.GetAllAppointmentsAsync();
            var upcomingAppointments = appointments
                .Where(a => 
                !a.ReminderSent &&
                a.AppointmentDate.Date == targetDate)
                .ToList();

            foreach (var appointment in upcomingAppointments)
            {
                if (appointment.Patient?.User?.Email != null)
                {
                    var message = @$"
                    <p>Olá {appointment.Patient.Names.Split(' ')[0]},</p>
                    <p>Lembramos que tens agendada uma consulta com <strong>{appointment.Staff.FullName}
                    </strong> no dia <strong>{appointment.AppointmentDate.ToShortDateString()}</strong> pelas {appointment.StartTime.ToString("HH\\hmm")}.</p>
                    <p>Local: <a href = ""https://maps.app.goo.gl/DNVAiw4m7ipYtE9h9"">Rua Camilo Castelo Branco, 2625-215 Póvoa de Santa Iria, Loja nº 27</a></p>                   
                    <p>Obrigado por contares connosco e até breve!</p>";

                    await _emailSender.SendEmailAsync(
                        appointment.Patient.User.Email,
                        "Lembrete de Consulta",
                        message
                    );

                    appointment.ReminderSent = true;
                    _context.Appointments.Update(appointment);
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}
