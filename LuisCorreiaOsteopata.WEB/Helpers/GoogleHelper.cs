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



    public async Task<Event> CreateEventAsync(User user, string calendarId, string title, string description, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var service = await GetCalendarServiceAsync(user, cancellationToken);

        var newEvent = new Event
        {
            Summary = title,
            Description = description,
            Start = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(start), TimeZone = "Europe/Lisbon" },
            End = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(end), TimeZone = "Europe/Lisbon" }
        };

        return await service.Events.Insert(newEvent, calendarId).ExecuteAsync(cancellationToken);
    }

    public async Task DeleteEventAsync(User user, string calendarId, string eventId, CancellationToken cancellationToken)
    {
        var service = await GetCalendarServiceAsync(user, cancellationToken);
        await service.Events.Delete(calendarId, eventId).ExecuteAsync(cancellationToken);
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

    public async Task<CalendarService> GetCalendarServiceAsync(User user, CancellationToken cancellationToken)
    {
        var credential = await GetCredentialAsync(user, cancellationToken);
        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = _settings.Value.ApplicationName
        });
    }

    public async Task<IList<CalendarListEntry>> GetUserCalendarsAsync(User user, CancellationToken cancellationToken)
    {
        var service = await GetCalendarServiceAsync(user, cancellationToken);
        var list = await service.CalendarList.List().ExecuteAsync(cancellationToken);

        return list.Items;
    }
}
