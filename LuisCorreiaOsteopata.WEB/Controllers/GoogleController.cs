using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuisCorreiaOsteopata.WEB.Controllers;

[Authorize]
public class GoogleController : Controller
{
    private readonly IUserHelper _userHelper;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<GoogleController> _logger;
    private readonly UserManager<User> _userManager;

    public GoogleController(IUserHelper userHelper,
        SignInManager<User> signInManager,
        ILogger<GoogleController> logger,
        UserManager<User> userManager)
    {
        _userHelper = userHelper;
        _signInManager = signInManager;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult LinkGoogleCalendar()
    {
        var currentUserEmail = User.Identity?.Name;
        _logger.LogInformation("User {Email} is initiating Google Calendar linking.", currentUserEmail);

        var redirectUrl = Url.Action("Callback");
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);

        _logger.LogInformation("Redirecting user {Email} to Google for calendar authorization.", currentUserEmail);
        return new ChallengeResult("Google", properties);
    }

    [HttpGet]
    public async Task<IActionResult> Callback()
    {
        var currentUserEmail = User.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("Received Google callback for user {Email}.", currentUserEmail);

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("Google login info is null for user {Email}. Cannot link account.", currentUserEmail);
            TempData["Error"] = "Não foi possível ligar a conta Google.";
            return RedirectToAction("Index", "Account");
        }

        var user = await _userHelper.GetCurrentUserAsync();
        _logger.LogInformation("Linking Google account {ProviderKey} to user {UserId} ({Email}).", info.ProviderKey, user.Id, user.Email);

        await _userManager.AddLoginAsync(user, info);

        var tokens = info.AuthenticationTokens.ToDictionary(t => t.Name, t => t.Value);

        foreach (var tokenName in new[] { "access_token", "refresh_token", "expires_at" })
        {
            if (tokens.TryGetValue(tokenName, out var tokenValue))
            {
                await _userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, tokenName, tokenValue);
                _logger.LogInformation("Stored Google token {TokenName} for user {UserId} ({Email}).", tokenName, user.Id, user.Email);
            }
            else
            {
                _logger.LogWarning("Google token {TokenName} not found for user {UserId} ({Email}).", tokenName, user.Id, user.Email);
            }
        }

        _logger.LogInformation("Successfully linked Google account for user {UserId} ({Email}).", user.Id, user.Email);
        return RedirectToAction("Index", "Account");
    }

    [HttpPost]
    public async Task<IActionResult> SavePreferredCalendar(string calendarId)
    {
        var user = await _userHelper.GetCurrentUserAsync();
        var previousCalendarId = user.CalendarId;
        user.CalendarId = calendarId;

        await _userHelper.UpdateUserAsync(user);

        _logger.LogInformation(
            "User {UserId} ({Email}) changed preferred calendar from {OldCalendarId} to {NewCalendarId}.",
            user.Id,
            user.Email,
            previousCalendarId,
            user.CalendarId
        );

        return RedirectToAction("Index", "Account");
    }
}
