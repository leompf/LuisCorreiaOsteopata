using LuisCorreiaOsteopata.WEB.Data.Entities;

namespace LuisCorreiaOsteopata.WEB.Models;

public class AppointmentViewModel
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int StaffId { get; set; }

    public string StaffName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public DateTime AppointmentDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string AppointmentStatus { get; set; } = string.Empty;

    public string? PatientNotes { get; set; }

    public string? StaffNotes { get; set; }

    public bool IsPaid { get; set; }
}
