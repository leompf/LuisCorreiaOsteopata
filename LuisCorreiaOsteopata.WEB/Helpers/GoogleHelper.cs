using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public class GoogleHelper : IGoogleHelper
{
    private readonly UserManager<User> _userManager;
    private readonly IOptions<GoogleSettings> _settings;
    private readonly ILogger<GoogleHelper> _logger;

    public GoogleHelper(UserManager<User> userManager,
        IOptions<GoogleSettings> settings,
        ILogger<GoogleHelper> logger)
    {
        _userManager = userManager;
        _settings = settings;
        _logger = logger;
    }


    #region Calendar Service
    public async Task<CalendarService?> GetCalendarServiceAsync(User user, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing Google Calendar service for user {Email}.", user.Email);

        var credential = await GetCredentialAsync(user, cancellationToken);
        if (credential == null)
        {
            _logger.LogWarning("Cannot create CalendarService for user {Email} because credential is null.", user.Email);
            return null;
        }

        var service = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = _settings.Value.ApplicationName
        });

        _logger.LogInformation("Google Calendar service initialized successfully for user {Email}.", user.Email);
        return service;
    }

    public async Task<IList<CalendarListEntry>> GetUserCalendarsAsync(User user, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching Google calendars for user {Email}.", user.Email);

        var service = await GetCalendarServiceAsync(user, cancellationToken);
        var list = await service.CalendarList.List().ExecuteAsync(cancellationToken);

        return list.Items;
    }

    #region CRUD Events
    public async Task<Event?> CreateEventAsync(User user, string calendarId, string title, string description, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating event '{Title}' in calendar {CalendarId} for user {Email}.", title, calendarId, user.Email);

        try
        {
            var service = await GetCalendarServiceAsync(user, cancellationToken);

            var newEvent = new Event
            {
                Summary = title,
                Description = description,
                Start = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(start), TimeZone = "Europe/Lisbon" },
                End = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(end), TimeZone = "Europe/Lisbon" }
            };

            var createdEvent = await service.Events.Insert(newEvent, calendarId).ExecuteAsync(cancellationToken);

            _logger.LogInformation("Event '{Title}' created successfully with ID {EventId} for user {Email}.", title, createdEvent.Id, user.Email);

            return createdEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create event '{Title}' in calendar {CalendarId} for user {Email}.", title, calendarId, user.Email);
            throw;
        }
    }

    public async Task DeleteEventAsync(User user, string calendarId, string eventId, CancellationToken cancellationToken)
    {
        var service = await GetCalendarServiceAsync(user, cancellationToken);
        _logger.LogInformation("Deleting event {EventId} in calendar {CalendarId} for user {Email}.", eventId, calendarId, user.Email);

        try
        {
            await service.Events.Delete(calendarId, eventId).ExecuteAsync(cancellationToken);
            _logger.LogInformation("Event {EventId} deleted successfully for user {Email}.", eventId, user.Email);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Event {EventId} not found for user {Email}, nothing to delete.", eventId, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete event {EventId} for user {Email}.", eventId, user.Email);
            throw;
        }
    }

    public async Task<Event?> UpdateEventAsync(User user, string calendarId, string eventId, string title, string description, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting update for event {EventId} in calendar {CalendarId} for user {Email}.", eventId, calendarId, user.Email);

        var service = await GetCalendarServiceAsync(user, cancellationToken);
        if (service == null)
        {
            _logger.LogWarning("Cannot update event {EventId} because CalendarService is null for user {Email}.", eventId, user.Email);
            return null;
        }

        try
        {
            var existingEvent = await service.Events.Get(calendarId, eventId).ExecuteAsync(cancellationToken);
            if (existingEvent == null)
            {
                _logger.LogWarning("Event {EventId} not found in calendar {CalendarId} for user {Email}.", eventId, calendarId, user.Email);
                return null;
            }

            existingEvent.Summary = title;
            existingEvent.Description = description;
            existingEvent.Start = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(start), TimeZone = "Europe/Lisbon" };
            existingEvent.End = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(end), TimeZone = "Europe/Lisbon" };

            var updatedEvent = await service.Events.Update(existingEvent, calendarId, eventId).ExecuteAsync(cancellationToken);
            _logger.LogInformation("Event {EventId} updated successfully in calendar {CalendarId} for user {Email}.", eventId, calendarId, user.Email);

            return updatedEvent;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Event {EventId} not found for user {Email}, nothing to update.", eventId, user.Email);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update event {EventId} in calendar {CalendarId} for user {Email}.", eventId, calendarId, user.Email);
            throw;
        }
    }
    #endregion

    #endregion

    #region Credentials
    private async Task<UserCredential> GetCredentialAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = await _userManager.GetAuthenticationTokenAsync(user, "Google", "access_token");
        var refreshToken = await _userManager.GetAuthenticationTokenAsync(user, "Google", "refresh_token");
        var expiryStr = await _userManager.GetAuthenticationTokenAsync(user, "Google", "expires_at");

        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("User {Email} has no Google account connected, skipping calendar operations.", user.Email);
            return null;
        }

        DateTime expiry;
        bool parsed = DateTime.TryParse(expiryStr, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out expiry);

        if (!parsed || expiry < DateTime.UtcNow.AddYears(-1) || expiry > DateTime.UtcNow.AddYears(10))
        {
            _logger.LogWarning("Invalid or missing expiry token for {Email}, using fallback.", user.Email);
            expiry = DateTime.UtcNow.AddHours(-1);
        }

        double totalSeconds = (expiry - DateTime.UtcNow).TotalSeconds;

        long? expiresInSeconds = (totalSeconds > 0) ? (long)totalSeconds : (long?)null;

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _settings.Value.ClientId,
                ClientSecret = _settings.Value.ClientSecret
            },
            Scopes = _settings.Value.Scope
        });

        var token = new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresInSeconds = expiresInSeconds
        };

        var credential = new UserCredential(flow, user.Id, token);

        if (credential.Token.IsStale)
        {
            _logger.LogInformation("Refreshing Google token for user {Email}", user.Email);
            var success = await credential.RefreshTokenAsync(cancellationToken);

            if (!success)
                throw new InvalidOperationException("Google token refresh failed.");

            await _userManager.SetAuthenticationTokenAsync(user, "Google", "access_token", credential.Token.AccessToken);
            await _userManager.SetAuthenticationTokenAsync(user, "Google", "expires_at",
                DateTime.UtcNow.AddSeconds(credential.Token.ExpiresInSeconds ?? 3600).ToString("o"));
        }

        return credential;
    }

    public async Task<bool> EnsureValidTokenAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            await GetCredentialAsync(user, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate Google token for {Email}", user.Email);
            return false;
        }
    }
    #endregion
}
