namespace LuisCorreiaOsteopata.WEB.Data.Entities
{
    public class GoogleCalendar : IEntity
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        public string UserId { get; set; } 

        public string EventId { get; set; }

        public string CalendarId { get; set; } 

        public string Role { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
