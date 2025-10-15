using Ganss.Xss;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Models;

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

    public Product ToProduct(ProductViewModel model, string path, bool isNew)
    {
        return new Product
        {
            Id = isNew ? 0 : model.Id,
            Name = model.Name,
            Description = _sanitizer.Sanitize(model.Description),
            IsAvailable = model.IsAvailable,
            LastPurchase = model.LastPurchase,
            LastSale = model.LastSale,
            ImageUrl = path,
            Price = model.Price,
            Stock = model.Stock,
            User = model.User
        };
    }

    public ProductViewModel ToProductViewModel(Product product)
    {
        return new ProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            IsAvailable = product.IsAvailable,
            LastPurchase = product.LastPurchase,
            LastSale = product.LastSale,
            ImageUrl = product.ImageUrl,
            Price = product.Price,
            Stock = product.Stock,
            User = product.User
        };
    }

    public UserViewModel ToUserViewModel(User user, string role)
    {
        return new UserViewModel
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
