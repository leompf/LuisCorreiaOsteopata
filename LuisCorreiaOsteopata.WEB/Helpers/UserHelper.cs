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
    private readonly ILogger<UserHelper> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserHelper(UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UserHelper> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    #region CRUD Users
    public async Task<List<User>> GetAllUsersAsync()
    {
        _logger.LogInformation("Fetching all users from the database.");

        try
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Names)
                .ToListAsync();

            _logger.LogInformation("Successfully fetched {UserCount} users.", users.Count);

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching all users.");
            throw;
        }
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogError("No active HTTP context.");
        }

        var email = httpContext.User?.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogError("No email claim found in the current user.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogError("User not found for email '{Email}'.", email);
            return null;
        }

        return user;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        _logger.LogInformation("Fetching user by email {Email}", email);

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("No user found with email {Email}", email);
            return null;
        }

        _logger.LogInformation("Successfully retrieved user {UserId} with email {Email}", user.Id, email);
        return user;
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        _logger.LogInformation("Fetching user by id {Id}", id);
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("No user found with id {Id}", id);
            return null;
        }

        _logger.LogInformation("Successfully retrieved user {UserId} with Id {Id}", user.Id, user.Id);
        return user;
    }

    public async Task<User?> GetUserByNifAsync(string nif)
    {
        _logger.LogInformation("Fetching user by NIF {Nif}", nif);
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Nif == nif);
        if (user == null)
        {
            _logger.LogWarning("No user found with NIF {Nif}", nif);
            return null;
        }

        _logger.LogInformation("Successfully retrieved user {UserId} with NIF {Nif}", user.Id, nif);
        return user;
    }

    public async Task<IdentityResult> AddUserAsync(User user, string password)
    {
        _logger.LogInformation("Adding new user {UserEmail}", user.Email);
        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserEmail} created successfully", user.Email);
        }
        else
        {
            _logger.LogWarning("Failed to create user {UserEmail}. Errors: {Errors}",
                user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return result;
    }

    public async Task<IdentityResult> UpdateUserAsync(User user)
    {
        _logger.LogInformation("Updating user {UserId}", user.Id);
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} updated successfully", user.Id);
        }
        else
        {
            _logger.LogWarning("Failed to update user {UserId}. Errors: {Errors}",
                user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return result;
    }

    public async Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword)
    {
        return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword)
    {
        return await _userManager.ResetPasswordAsync(user, token, newPassword);
    }
    #endregion

    #region CRUD Roles
    public IEnumerable<SelectListItem> GetAllRolesAsync()
    {
        _logger.LogInformation("Fetching all roles from the database.");

        try
        {
            var roles = _roleManager.Roles.ToList();
            _logger.LogInformation("Successfully retrieved {RoleCount} roles.", roles.Count);

            return roles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching roles.");
            throw;
        }
    }

    public async Task AddUserToRoleAsync(User user, string rolename)
    {
        _logger.LogInformation("Adding user {UserId} ({Email}) to role {RoleName}", user.Id, user.Email, rolename);

        try
        {
            var result = await _userManager.AddToRoleAsync(user, rolename);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} successfully added to role {RoleName}", user.Id, rolename);
            }
            else
            {
                _logger.LogWarning("Failed to add user {UserId} to role {RoleName}. Errors: {Errors}",
                    user.Id, rolename, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while adding user {UserId} to role {RoleName}", user.Id, rolename);
            throw;
        }
    }

    public async Task CheckRoleAsync(string roleName)
    {
        _logger.LogInformation("Checking if role {RoleName} exists.", roleName);

        try
        {
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                _logger.LogInformation("Role {RoleName} does not exist. Creating role.", roleName);
                var result = await _roleManager.CreateAsync(new IdentityRole { Name = roleName });

                if (result.Succeeded)
                {
                    _logger.LogInformation("Role {RoleName} created successfully.", roleName);
                }
                else
                {
                    _logger.LogWarning("Failed to create role {RoleName}. Errors: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogInformation("Role {RoleName} already exists.", roleName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while checking or creating role {RoleName}", roleName);
            throw;
        }
    }

    public async Task<string> GetUserRoleAsync(User user)
    {
        _logger.LogInformation("Fetching roles for user {UserId} ({Email})", user.Id, user.Email);

        try
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            if (role != null)
                _logger.LogInformation("User {UserId} is in role {RoleName}", user.Id, role);
            else
                _logger.LogWarning("User {UserId} does not have any roles assigned", user.Id);

            return role!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while fetching roles for user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> IsUserInRoleAsync(User user, string rolename)
    {
        _logger.LogInformation("Checking if user {UserId} is in role {RoleName}", user.Id, rolename);

        try
        {
            var result = await _userManager.IsInRoleAsync(user, rolename);
            _logger.LogInformation("User {UserId} is {InRole} role {RoleName}",
                user.Id, result ? "in" : "not in", rolename);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while checking role {RoleName} for user {UserId}", rolename, user.Id);
            throw;
        }
    }
    #endregion

    #region 2FA Authentication
    public async Task<string?> GetAuthenticatorKeyAsync(User user)
    {
        _logger.LogInformation("Retrieving authenticator key for user {UserId} ({Email})", user.Id, user.Email);

        try
        {
            var key = await _userManager.GetAuthenticatorKeyAsync(user);

            if (key != null)
                _logger.LogDebug("Authenticator key retrieved for user {UserId}", user.Id);
            else
                _logger.LogWarning("No authenticator key found for user {UserId}", user.Id);

            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authenticator key for user {UserId}", user.Id);
            throw;
        }
    }

    public async Task ResetAuthenticatorKeyAsync(User user)
    {
        _logger.LogInformation("Resetting authenticator key for user {UserId} ({Email})", user.Id, user.Email);

        try
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            _logger.LogInformation("Authenticator key reset successfully for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting authenticator key for user {UserId}", user.Id);
            throw;
        }
    }

    public async Task SetTwoFactorEnabledAsync(User user, bool enabled)
    {
        _logger.LogInformation("{Action} 2FA for user {UserId} ({Email})", enabled ? "Enabling" : "Disabling", user.Id, user.Email);

        try
        {
            await _userManager.SetTwoFactorEnabledAsync(user, enabled);
            _logger.LogInformation("2FA {Status} for user {UserId}", enabled ? "enabled" : "disabled", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting 2FA status for user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> VerifyTwoFactorTokenAsync(User user, string code)
    {
        _logger.LogInformation("Verifying 2FA token for user {UserId} ({Email})", user.Id, user.Email);

        try
        {
            var result = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

            _logger.LogInformation("2FA token verification for user {UserId} {Outcome}", user.Id, result ? "succeeded" : "failed");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying 2FA token for user {UserId}", user.Id);
            throw;
        }
    }
    #endregion

    #region Login
    public async Task<SignInResult> LoginAsync(string username, string password, bool rememberMe)
    {
        _logger.LogInformation("Attempting login for user {Username}", username);

        try
        {
            var result = await _signInManager.PasswordSignInAsync(
                username,
                password,
                isPersistent: rememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Username} logged in successfully.", username);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Username} is locked out.", username);
            }
            else if (result.RequiresTwoFactor)
            {
                _logger.LogInformation("User {Username} requires 2FA.", username);
            }
            else
            {
                _logger.LogWarning("Failed login attempt for user {Username}.", username);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during login for user {Username}", username);
            throw;
        }
    }

    public async Task LogoutAsync()
    {
        var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Anonymous";
        _logger.LogInformation("User {Username} is logging out.", username);

        try
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {Username} logged out successfully.", username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during logout for user {Username}", username);
            throw;
        }
    }

    public async Task<bool> CheckPasswordAsync(User user, string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }
    #endregion

    #region External
    public async Task StoreUserTokenAsync(User user, string loginProvider, string name, string value)
    {
        _logger.LogInformation(
        "Storing authentication token for user {UserId} ({Email}) with provider {Provider} and token name {TokenName}",
        user.Id, user.Email, loginProvider, name);

        try
        {
            await _userManager.RemoveAuthenticationTokenAsync(user, loginProvider, name);
            _logger.LogDebug("Removed existing token (if any) for user {UserId}, provider {Provider}, token name {TokenName}",
                user.Id, loginProvider, name);

            await _userManager.SetAuthenticationTokenAsync(user, loginProvider, name, value);
            _logger.LogInformation(
                "Token successfully stored for user {UserId}, provider {Provider}, token name {TokenName}",
                user.Id, loginProvider, name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred while storing token for user {UserId}, provider {Provider}, token name {TokenName}",
                user.Id, loginProvider, name);
            throw;
        }
    }

    public async Task<string?> GetUserTokenAsync(User user, string loginProvider, string tokenName)
    {
        if (user == null)
        {
            _logger.LogWarning("GetUserTokenAsync called with null user for provider {Provider} and token name {TokenName}",
                loginProvider, tokenName);
            return null;
        }

        _logger.LogInformation(
            "Retrieving authentication token for user {UserId} ({Email}) with provider {Provider} and token name {TokenName}",
            user.Id, user.Email, loginProvider, tokenName);

        try
        {
            var token = await _userManager.GetAuthenticationTokenAsync(user, loginProvider, tokenName);

            if (token != null)
                _logger.LogDebug("Token exists for user {UserId}, provider {Provider}, token name {TokenName}",
                    user.Id, loginProvider, tokenName);
            else
                _logger.LogWarning("No token found for user {UserId}, provider {Provider}, token name {TokenName}",
                    user.Id, loginProvider, tokenName);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred while retrieving token for user {UserId}, provider {Provider}, token name {TokenName}",
                user.Id, loginProvider, tokenName);
            throw;
        }
    }

    public async Task<IdentityResult> AddExternalLoginAsync(User user, ExternalLoginInfo info)
    {
        if (user == null) 
            throw new ArgumentNullException(nameof(user));

        if (info == null) 
            throw new ArgumentNullException(nameof(info));

        var login = new UserLoginInfo(
            info.LoginProvider,
            info.ProviderKey,
            info.ProviderDisplayName);

        return await _userManager.AddLoginAsync(user, login);
    }

    public async Task<UserLoginInfo?> GetExternalLoginAsync(User user, string loginProvider)
    {
        if (user == null) 
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrEmpty(loginProvider))
            throw new ArgumentNullException(nameof(loginProvider));

        var logins = await _userManager.GetLoginsAsync(user);
        return logins.FirstOrDefault(l => l.LoginProvider == loginProvider);
    }
    #endregion

    #region Helpers
    public async Task<IdentityResult> ConfirmEmailAsync(User user, string token)
    {
        _logger.LogInformation("Confirming email for user {UserId} ({Email})", user.Id, user.Email);

        try
        {
            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
                _logger.LogInformation("Email confirmed successfully for user {UserId}", user.Id);
            else
                _logger.LogWarning("Email confirmation failed for user {UserId}. Errors: {Errors}",
                    user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while confirming email for user {UserId}", user.Id);
            throw;
        }
    }

    public List<UserViewModel> FilterUsers(IEnumerable<UserViewModel> users, string? nameFilter, string? emailFilter, string? phoneFilter, string? nifFilter)
    {
        _logger.LogInformation("Filtering users with Name='{Name}', Email='{Email}', Phone='{Phone}', NIF='{NIF}'",
        nameFilter, emailFilter, phoneFilter, nifFilter);

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

        _logger.LogInformation("Filtered users count: {Count}", filtered.Count);
        return filtered;
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(User user)
    {
        _logger.LogInformation("Generating email confirmation token for user {UserId} ({Email})", user.Id, user.Email);

        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            _logger.LogDebug("Email confirmation token generated for user {UserId}", user.Id);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while generating email confirmation token for user {UserId}", user.Id);
            throw;
        }
    }

    public string GenerateRandomPassword(PasswordOptions options)
    {
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

    public async Task<string> GeneratePasswordResetTokenAsync(User user)
    {
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }


    #endregion
}
