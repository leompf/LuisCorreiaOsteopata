namespace LuisCorreiaOsteopata.WEB.Data.Entities;

public class AppointmentCredit : IEntity
{
    public int Id { get; set; }

    public int TotalAppointments { get; set; }

    public int UsedAppointments { get; set; } 

    public bool IsActive => UsedAppointments < TotalAppointments;

    public DateTime CreatedAt { get; set; }

    //FK
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;


    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
