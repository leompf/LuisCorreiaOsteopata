using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuisCorreiaOsteopata.WEB.Controllers;

[Authorize]
public class GoogleController : Controller
{
    private readonly IUserHelper _userHelper;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public GoogleController(IUserHelper userHelper,
        SignInManager<User> signInManager,
        UserManager<User> userManager)
    {
        _userHelper = userHelper;
        _signInManager = signInManager;
        _userManager = userManager;
    }


    public IActionResult LinkGoogleCalendar()
    {
        var redirectUrl = Url.Action("Callback");
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
        return new ChallengeResult("Google", properties);
    }


    public async Task<IActionResult> Callback()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["Error"] = "Não foi possível ligar a conta Google.";
            return RedirectToAction("Profile", "Account");
        }

        var user = await _userHelper.GetCurrentUserAsync();
        await _userManager.AddLoginAsync(user, info);

        await _userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "access_token", info.AuthenticationTokens.FirstOrDefault(t => t.Name == "access_token")?.Value);
        await _userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "refresh_token", info.AuthenticationTokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value);
        await _userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "expires_at", info.AuthenticationTokens.FirstOrDefault(t => t.Name == "expires_at")?.Value);

        return RedirectToAction("Profile", "Account");
    }


    [HttpPost]
    public async Task<IActionResult> SavePreferredCalendar(ProfileViewModel model)
    {
        var user = await _userHelper.GetCurrentUserAsync();
        user.CalendarId = model.CalendarId;
        await _userHelper.UpdateUserAsync(user);

        TempData["Message"] = "Calendário preferido guardado com sucesso!";
        return RedirectToAction("Profile", "Account");
    }
}
