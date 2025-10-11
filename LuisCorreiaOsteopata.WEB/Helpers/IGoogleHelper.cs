using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public interface IGoogleHelper
{
    Task<CalendarService> GetCalendarServiceAsync(User user, CancellationToken cancellationToken);

    Task<IList<CalendarListEntry>> GetUserCalendarsAsync(User user, CancellationToken cancellationToken);

    Task<Event> CreateEventAsync(User user, string calendarId, string title, string description, DateTime start, DateTime end, CancellationToken cancellationToken);

    Task DeleteEventAsync(User user, string calendarId, string eventId, CancellationToken cancellationToken);

    Task<bool> EnsureValidTokenAsync(User user, CancellationToken cancellationToken);
}
