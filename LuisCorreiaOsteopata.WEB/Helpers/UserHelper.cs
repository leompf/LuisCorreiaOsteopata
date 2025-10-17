using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public class UserHelper : IUserHelper
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserHelper(UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<IdentityRole> roleManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IdentityResult> AddUserAsync(User user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task AddUserToRoleAsync(User user, string rolename)
    {
        await _userManager.AddToRoleAsync(user, rolename);
    }

    public async Task CheckRoleAsync(string roleName)
    {
        var roleExists = await _roleManager.RoleExistsAsync(roleName);

        if (!roleExists)
        {
            await _roleManager.CreateAsync(new IdentityRole
            {
                Name = roleName
            });
        }
    }

    public async Task<IdentityResult> ConfirmEmailAsync(User user, string token)
    {
        return await _userManager.ConfirmEmailAsync(user, token);
    }

    public List<UserViewModel> FilterUsers(IEnumerable<UserViewModel> users, string? nameFilter, string? emailFilter, string? phoneFilter, string? nifFilter)
    {
        var filtered = users.ToList();

        if (!string.IsNullOrEmpty(nameFilter))
            filtered = filtered
                .Where(u => !string.IsNullOrEmpty(u.Name) &&
                            u.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (!string.IsNullOrEmpty(emailFilter))
            filtered = filtered
                .Where(u => !string.IsNullOrEmpty(u.Email) &&
                            u.Email.Contains(emailFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (!string.IsNullOrEmpty(phoneFilter))
            filtered = filtered
                .Where(u => !string.IsNullOrEmpty(u.PhoneNumber) &&
                            u.PhoneNumber.Contains(phoneFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (!string.IsNullOrEmpty(nifFilter))
            filtered = filtered
                .Where(u => !string.IsNullOrEmpty(u.NIF) &&
                            u.NIF.Contains(nifFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

        return filtered;
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(User user)
    {
        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public string GenerateRandomPassword(PasswordOptions options = null)
    {
        if (options == null) options = new PasswordOptions()
        {
            RequiredLength = 6,
            RequiredUniqueChars = 4,
            RequireDigit = true,
            RequireLowercase = true,
            RequireNonAlphanumeric = true,
            RequireUppercase = true
        };

        string[] randomChars = new[] {
            "ABCDEFGHJKLMNOPQRSTUVWXYZ",
            "abcdefghijkmnopqrstuvwxyz",
            "0123456789",
            "!@$?_-"
        };

        Random rand = new Random(Environment.TickCount);
        List<char> chars = new List<char>();

        if (options.RequireUppercase)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[0][rand.Next(0, randomChars[0].Length)]);

        if (options.RequireLowercase)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[1][rand.Next(0, randomChars[1].Length)]);

        if (options.RequireDigit)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[2][rand.Next(0, randomChars[2].Length)]);

        if (options.RequireNonAlphanumeric)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[3][rand.Next(0, randomChars[3].Length)]);

        for (int i = chars.Count; i < options.RequiredLength
            || chars.Distinct().Count() < options.RequiredUniqueChars; i++)
        {
            string rcs = randomChars[rand.Next(0, randomChars.Length)];
            chars.Insert(rand.Next(0, chars.Count),
                rcs[rand.Next(0, rcs.Length)]);
        }

        return new string(chars.ToArray());
    }

    public IEnumerable<SelectListItem> GetAllRolesAsync()
    {
        var roles = _roleManager.Roles.ToList();

        return roles.Select(r => new SelectListItem
        {
            Value = r.Name,
            Text = r.Name
        });
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        return await _userManager.Users
            .OrderBy(u => u.Names)
            .ToListAsync();
    }

    public async Task<string> GetAuthenticatorKeyAsync(User user)
    {
        return await _userManager.GetAuthenticatorKeyAsync(user);
    }

    public async Task<User> GetCurrentUserAsync()
    {
        var email = _httpContextAccessor.HttpContext.User?.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        return await GetUserByEmailAsync(email);
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<User> GetUserByIdAsync(string id)
    {
        return await _userManager.FindByIdAsync(id);
    }

    public async Task<User> GetUserByNifAsync(string nif)
    {
        var user = _userManager.Users.FirstOrDefault(u => u.Nif == nif);
        return user;
    }

    public async Task<string> GetUserRoleAsync(User user)
    {
        var role = await _userManager.GetRolesAsync(user);
        return role.FirstOrDefault();
    }

    public async Task<string> GetUserTokenAsync(User user, string loginProvider, string tokenName)
    {
        if (user == null)
            return null;

        var token = await _userManager.GetAuthenticationTokenAsync(user, loginProvider, tokenName);
        return token;
    }

    public async Task<bool> IsUserInRoleAsync(User user, string rolename)
    {
        return await _userManager.IsInRoleAsync(user, rolename);
    }

    public async Task<SignInResult> LoginAsync(string username, string password, bool rememberMe)
    {
        return await _signInManager.PasswordSignInAsync(
            username,
            password,
            isPersistent: rememberMe,
            lockoutOnFailure: false
        );
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task ResetAuthenticatorKeyAsync(User user)
    {
        await _userManager.ResetAuthenticatorKeyAsync(user);
    }

    public async Task SetTwoFactorEnabledAsync(User user, bool enabled)
    {
        await _userManager.SetTwoFactorEnabledAsync(user, enabled);
    }

    public List<UserViewModel> SortUsers(IEnumerable<UserViewModel> users, string? sortBy, bool sortDescending)
    {
        return sortBy switch
        {
            "Name" => sortDescending ? users.OrderByDescending(u => u.Name).ToList() : users.OrderBy(u => u.Name).ToList(),
            "Birthdate" => sortDescending ? users.OrderByDescending(u => u.Birthdate).ToList() : users.OrderBy(u => u.Birthdate).ToList(),
            "NIF" => sortDescending ? users.OrderByDescending(u => u.NIF).ToList() : users.OrderBy(u => u.NIF).ToList(),
            "Email" => sortDescending ? users.OrderByDescending(u => u.Email).ToList() : users.OrderBy(u => u.Email).ToList(),
            "PhoneNumber" => sortDescending ? users.OrderByDescending(u => u.PhoneNumber).ToList() : users.OrderBy(u => u.PhoneNumber).ToList(),
            "Role" => sortDescending ? users.OrderByDescending(u => u.Role).ToList() : users.OrderBy(u => u.Role).ToList(),
            _ => sortDescending ? users.OrderByDescending(u => u.Name).ToList() : users.OrderBy(u => u.Name).ToList()
        };
    }

    public async Task StoreUserTokenAsync(User user, string loginProvider, string name, string value)
    {
        await _userManager.RemoveAuthenticationTokenAsync(user, loginProvider, name);

        await _userManager.SetAuthenticationTokenAsync(user, loginProvider, name, value);
    }

    public async Task<IdentityResult> UpdateUserAsync(User user)
    {
        return await _userManager.UpdateAsync(user);
    }

    public async Task<bool> VerifyTwoFactorTokenAsync(User user, string code)
    {
        return await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);      
    }
}
