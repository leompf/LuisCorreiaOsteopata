using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Models;

namespace LuisCorreiaOsteopata.WEB.Helpers
{
    public interface IConverterHelper
    {
        AppointmentViewModel ToAppointmentViewModel(Appointment appointment);

        string Sanitize(string html);
    }
}
