namespace LuisCorreiaOsteopata.WEB.Models;

public class AppointmentListViewModel
{
    public string? StaffName { get; set; }
    public string? PatientName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public IEnumerable<AppointmentViewModel>? Appointments { get; set; }
}
