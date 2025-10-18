using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public interface IGoogleHelper
{
    #region Calendar Service
    Task<CalendarService?> GetCalendarServiceAsync(User user, CancellationToken cancellationToken);

    Task<IList<CalendarListEntry>> GetUserCalendarsAsync(User user, CancellationToken cancellationToken);

    #region CRUD Events
    Task<Event?> CreateEventAsync(User user, string calendarId, string title, string description, DateTime start, DateTime end, CancellationToken cancellationToken);
    Task<Event?> UpdateEventAsync(User user, string calendarId, string eventId, string title, string description, DateTime start, DateTime end, CancellationToken cancellationToken);
    Task DeleteEventAsync(User user, string calendarId, string eventId, CancellationToken cancellationToken);

    #endregion

    #endregion

    #region Credentials
    Task<bool> EnsureValidTokenAsync(User user, CancellationToken cancellationToken);

    #endregion
}
