using LuisCorreiaOsteopata.WEB.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LuisCorreiaOsteopata.WEB.Helpers
{
    public class ReminderHelper : IReminderHelper
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ReminderHelper> _logger;
        private readonly DataContext _context;

        private readonly string TwilioAccountSid;
        private readonly string TwilioAuthToken;

        public ReminderHelper(IAppointmentRepository appointmentRepository,
            IEmailSender emailSender,
            ILogger<ReminderHelper> logger,
            IConfiguration configuration,
            DataContext context)
        {
            _appointmentRepository = appointmentRepository;
            _emailSender = emailSender;
            _logger = logger;
            _context = context;

            TwilioAccountSid = configuration["Twilio:AccountSid"];
            TwilioAuthToken = configuration["Twilio:AuthToken"];
        }
        public async Task SendAppointmentReminderAsync()
        {
            Twilio.TwilioClient.Init(TwilioAccountSid, TwilioAuthToken);
            var targetDate = DateTime.UtcNow.AddHours(24).Date;

            var appointments = _appointmentRepository.GetAllAppointments();
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
                }

                if (!string.IsNullOrEmpty(appointment.Patient?.User.PhoneNumber))
                {                    
                    var smsMessage = $"Olá {appointment.Patient.Names.Split(' ')[0]}, lembramos que tens " +
                        $"agendada uma consulta com {appointment.Staff.FullName} no dia {appointment.AppointmentDate.ToShortDateString()} pelas pelas {appointment.StartTime.ToString("HH\\hmm")}." +
                        $"Obrigado por contares connosco e até breve!";

                    try
                    {
                        var message = MessageResource.Create(
                        body: smsMessage,
                        from: new PhoneNumber("+15005550006"),
                        to: new PhoneNumber(appointment.Patient.User.PhoneNumber)
                        );

                        _logger.LogInformation($"Test SMS sent successfully: {message.Sid}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to send SMS: {ex.Message}");
                    }                  
                }

                appointment.ReminderSent = true;
                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();
            }
        }
    }
}
