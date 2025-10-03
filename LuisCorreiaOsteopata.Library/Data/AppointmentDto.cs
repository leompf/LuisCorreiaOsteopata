namespace LuisCorreiaOsteopata.Library.Data
{
    public class AppointmentDto
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int StaffId { get; set; }
    }
}
