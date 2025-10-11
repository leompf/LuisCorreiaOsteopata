namespace LuisCorreiaOsteopata.WEB.Helpers
{
    public class GoogleSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = "LuisCorreiaOsteopata";
        public string CalendarId { get; set; } = "primary";

        public string[] Scope { get; set; } =
        {
            "https://www.googleapis.com/auth/calendar",
            "https://www.googleapis.com/auth/calendar.events",
            "email",
            "profile"
        };


        public string User { get; set; } = "user";
    }
}

