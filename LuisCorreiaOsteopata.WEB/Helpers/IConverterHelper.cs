using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Models;

namespace LuisCorreiaOsteopata.WEB.Helpers
{
    public interface IConverterHelper
    {
        AppointmentViewModel ToAppointmentViewModel(Appointment appointment);

        UserViewModel ToUserViewModel(User user, string role);

        Product ToProduct(ProductViewModel model, string path, bool isNew);

        ProductViewModel ToProductViewModel(Product product);

        string Sanitize(string html);
    }
}
