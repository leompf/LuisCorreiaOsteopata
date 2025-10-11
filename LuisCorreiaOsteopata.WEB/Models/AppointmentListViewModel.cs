using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.WEB.Models;

public class AppointmentListViewModel
{
    public int? StaffId { get; set; } 
    public int? PatientId { get; set; } 
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public IEnumerable<SelectListItem>? StaffMembers { get; set; } 
    public IEnumerable<SelectListItem>? Patients { get; set; } 
    public IEnumerable<AppointmentViewModel>? Appointments { get; set; } 
}
